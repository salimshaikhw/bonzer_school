using Application.Fees;
using Application.Abstractions;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common;
using Application.Exceptions;
using Application.ViewModels.Payment;
using AutoMapper;
using Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Fees
{
    public class PaymentService : IPaymentService
    {
        private readonly ILogger<PaymentService> _logger;
        private readonly IPaymentRepository _paymentRepo;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly BonzerSchoolDbContext _context;

        private readonly TimeProvider _clock;

        public PaymentService(
            ILogger<PaymentService> logger,
            IPaymentRepository paymentRepo,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            BonzerSchoolDbContext context,
            TimeProvider clock)
        {
            _clock = clock;
            _logger = logger;
            _paymentRepo = paymentRepo;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _context = context;
        }

        public async Task<IEnumerable<PaymentResponse>> GetAllAsync()
        {
            _logger.LogDebug("Fetching all Payments");
            var entities = await _paymentRepo.GetAllAsync();
            return _mapper.Map<IEnumerable<PaymentResponse>>(entities);
        }

        public async Task<PaymentResponse> GetByIdAsync(Guid id)
        {
            _logger.LogDebug("Fetching Payment by id: {Id}", id);
            var entity = await _paymentRepo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<PaymentResponse>(entity);
        }

        /// <summary>
        /// Records a fee payment. Rules (all checked before anything is saved, then the payment and its fee lines
        /// are written in one transaction, so a bad line can never leave a payment without its breakdown):
        ///   - the student exists; the payment's branch / division are the student's;
        ///   - the amount is positive and, with fee lines, equals their sum; every line is positive and names
        ///     an existing fee head or a custom fee;
        ///   - the payment date is not in the future.
        /// A unique 12-digit receipt number is assigned.
        /// </summary>
        public async Task<PaymentResponse> CreateAsync(PaymentRequest request)
        {
            var student = await _context.Students.AsNoTracking().FirstOrDefaultAsync(st => st.Id == request.StudentId)
                          ?? throw new BaseServiceException($"Student {request.StudentId} does not exist.", ExceptionCodes.Validation);
            if (request.BranchId != null && student.BranchId != null && request.BranchId != student.BranchId)
                throw new BaseServiceException("The payment's branch must be the student's branch.", ExceptionCodes.Validation);
            if (request.PaymentDate is { } paidOn && paidOn > DateOnly.FromDateTime(_clock.GetUtcNow().UtcDateTime).AddDays(1))
                throw new BaseServiceException("The payment date cannot be in the future.", ExceptionCodes.Validation);

            var lines = await ValidateFeeLinesAsync(request.FeeLines);
            var amount = lines.Count > 0 ? lines.Sum(l => l.Amount) : request.Amount ?? 0;
            if (lines.Count > 0 && request.Amount is { } stated && Math.Abs(stated - amount) > 0.01m)
                throw new BaseServiceException($"The payment amount ({stated}) must equal the sum of its fee lines ({amount}).", ExceptionCodes.Validation);
            if (amount <= 0)
                throw new BaseServiceException("The payment amount must be greater than zero.", ExceptionCodes.Validation);

            var entity = _mapper.Map<Payment>(request);
            entity.Id = Guid.NewGuid();
            entity.Amount = amount;
            entity.BranchId = student.BranchId ?? request.BranchId;
            entity.DivisionId = request.DivisionId ?? student.DivisionId;
            entity.InvoiceId = await GenerateUniqueInvoiceNumberAsync();

            _context.Payments.Add(entity);
            foreach (var line in lines)
            {
                line.PaymentId = entity.Id;
                _context.PaymentFeeLines.Add(line);
            }
            await SettleInvoiceItemsAsync(lines, student.Id, reverse: false);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Payment {PaymentId} of {Amount} recorded for student {StudentId} with receipt {ReceiptNo}",
                entity.Id, amount, student.Id, entity.InvoiceId);
            return _mapper.Map<PaymentResponse>(entity);
        }

        /// <summary>
        /// A recorded payment is a financial record: only its descriptive fields (method, type, transaction
        /// number, bank, remarks, collector) can be corrected. Amount, student, date and fee lines cannot; delete
        /// the payment and record it again instead.
        /// </summary>
        public async Task<PaymentResponse> UpdateAsync(Guid id, PaymentRequest request)
        {
            var entity = await _paymentRepo.GetByIdAsync(id);
            if ((request.Amount is { } amount && amount != entity.Amount)
                || (request.StudentId != null && request.StudentId != entity.StudentId)
                || (request.PaymentDate != null && request.PaymentDate != entity.PaymentDate)
                || request.FeeLines is { Length: > 0 })
                throw new BaseServiceException(
                    "The amount, student, date and fee lines of a recorded payment cannot be changed. Delete it and record the payment again.",
                    ExceptionCodes.Operation);

            entity.PaymentMethod = request.PaymentMethod ?? entity.PaymentMethod;
            entity.PaymentType = request.PaymentType ?? entity.PaymentType;
            entity.TransactionNumber = request.TransactionNumber ?? entity.TransactionNumber;
            entity.BankName = request.BankName ?? entity.BankName;
            entity.Remarks = request.Remarks ?? entity.Remarks;
            entity.CollectedByStaffId = request.CollectedByStaffId ?? entity.CollectedByStaffId;

            await _paymentRepo.UpdateAsync(entity);
            return _mapper.Map<PaymentResponse>(entity);
        }

        /// <summary>Deletes the payment together with its fee lines, in one transaction.</summary>
        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _paymentRepo.GetByIdAsync(id);
            var lines = await _context.PaymentFeeLines.Where(l => l.PaymentId == entity.Id).ToListAsync();
            await SettleInvoiceItemsAsync(lines, entity.StudentId, reverse: true);
            _context.PaymentFeeLines.RemoveRange(lines);
            _context.Payments.Remove(entity);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Payment {PaymentId} of {Amount} deleted (receipt {ReceiptNo})", entity.Id, entity.Amount, entity.InvoiceId);
            return true;
        }

        /// <summary>
        /// Applies (or, for a deleted payment, reverses) the lines that name an invoice item to that item and its
        /// invoice (InvoiceSettlement). The items must belong to an invoice of the paying student; a line cannot pay
        /// more than the item still owes. Changes are saved with the payment.
        /// </summary>
        private async Task SettleInvoiceItemsAsync(IReadOnlyCollection<PaymentFeeLine> lines, Guid? studentId, bool reverse)
        {
            var itemIds = lines.Where(l => l.InvoiceItemId != null).Select(l => l.InvoiceItemId!.Value).Distinct().ToList();
            if (itemIds.Count == 0)
                return;

            var items = await _context.InvoiceItems.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id);
            var invoiceIds = items.Values.Select(i => i.InvoiceId).Distinct().ToList();
            var invoices = await _context.Invoices.Where(i => invoiceIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id);

            foreach (var line in lines.Where(l => l.InvoiceItemId != null))
            {
                if (!items.TryGetValue(line.InvoiceItemId!.Value, out var item))
                    throw new BaseServiceException($"Invoice item {line.InvoiceItemId} does not exist.", ExceptionCodes.Validation);
                if (!reverse && invoices.GetValueOrDefault(item.InvoiceId)?.StudentId != studentId)
                    throw new BaseServiceException("The invoice belongs to another student.", ExceptionCodes.Validation);
                line.FeeHeadId ??= item.FeeHeadId;
                if (reverse) InvoiceSettlement.Reverse(item, line.Amount);
                else InvoiceSettlement.Apply(item, line.Amount);
            }

            var allItems = await _context.InvoiceItems.Where(i => invoiceIds.Contains(i.InvoiceId)).ToListAsync();
            foreach (var invoice in invoices.Values)
                InvoiceSettlement.Recalculate(invoice, allItems.Where(i => i.InvoiceId == invoice.Id));
        }

        private async Task<List<PaymentFeeLine>> ValidateFeeLinesAsync(PaymentFeeLineRequest[]? requested)
        {
            var lines = new List<PaymentFeeLine>();
            if (requested == null || requested.Length == 0)
                return lines;

            var headIds = requested.Where(l => l.FeeHeadId.HasValue).Select(l => l.FeeHeadId!.Value).Distinct().ToList();
            var knownHeads = await _context.FeeHeads.AsNoTracking().Where(h => headIds.Contains(h.Id)).Select(h => h.Id).ToListAsync();
            foreach (var line in requested)
            {
                if ((line.Amount ?? 0) <= 0)
                    throw new BaseServiceException("Each fee line amount must be greater than zero.", ExceptionCodes.Validation);
                if (!line.FeeHeadId.HasValue && !line.InvoiceItemId.HasValue && string.IsNullOrWhiteSpace(line.CustomFeeName))
                    throw new BaseServiceException("Each fee line needs a feeHeadId or a customFeeName.", ExceptionCodes.Validation);
                if (line.FeeHeadId is { } headId && !knownHeads.Contains(headId))
                    throw new BaseServiceException($"Fee head {headId} does not exist.", ExceptionCodes.Validation);
                lines.Add(new PaymentFeeLine
                {
                    Id = Guid.NewGuid(),
                    FeeHeadId = line.FeeHeadId,
                    InvoiceItemId = line.InvoiceItemId,
                    Amount = line.Amount!.Value,
                    CustomFeeName = string.IsNullOrWhiteSpace(line.CustomFeeName) ? null : line.CustomFeeName.Trim(),
                });
            }
            return lines;
        }

        public async Task<PaymentReceiptDto?> GetReceiptAsync(Guid paymentId)
        {
            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.Id == paymentId);
            if (payment == null) return null;

            var student = payment.StudentId.HasValue
                ? await _context.Students.FirstOrDefaultAsync(s => s.Id == payment.StudentId.Value)
                : null;

            var branch = payment.BranchId.HasValue
                ? await _context.Branches.FirstOrDefaultAsync(b => b.Id == payment.BranchId.Value)
                : null;

            var pflRows = await _context.PaymentFeeLines
                .Where(pfl => pfl.PaymentId == paymentId)
                .ToListAsync();

            var headIds = pflRows.Where(r => r.FeeHeadId.HasValue).Select(r => r.FeeHeadId!.Value).Distinct().ToList();

            var headNames = headIds.Count == 0
                ? new Dictionary<Guid, string>()
                : await _context.FeeHeads.AsNoTracking()
                    .Where(fh => headIds.Contains(fh.Id))
                    .ToDictionaryAsync(fh => fh.Id, fh => fh.Name ?? "");

            var feeLines = pflRows.Select(pfl => new ReceiptFeeLineDto
            {
                FeeHeadName = pfl.FeeHeadId.HasValue
                    ? headNames.GetValueOrDefault(pfl.FeeHeadId.Value, "Unknown Fee")
                    : (pfl.CustomFeeName ?? "Other"),
                Amount = pfl.Amount
            }).ToArray();

            return new PaymentReceiptDto
            {
                PaymentId = payment.Id,
                InvoiceId = payment.InvoiceId, // Already a string
                PaymentDate = payment.PaymentDate?.ToDateTime(TimeOnly.MinValue),
                Amount = payment.Amount,
                PaymentMethod = payment.PaymentMethod,
                TransactionNumber = payment.TransactionNumber,
                BankName = payment.BankName,
                Remarks = payment.Remarks,
                CreatedAt = payment.CreatedAt,
                RollNumber = student?.RollNumber,
                ClassId = student?.ClassId,
                DivisionId = student?.DivisionId,
                BranchName = branch?.Name,
                FeeLines = feeLines
            };
        }

        /// <summary>
        /// Generates a random 10-digit number and checks database for uniqueness
        /// </summary>
        private async Task<string> GenerateUniqueInvoiceNumberAsync()
        {
            string code = string.Empty;
            bool isUnique = false;

            while (!isUnique)
            {
                // Generates a 10-digit number string
                // Range: 100,000,000,000 to 999,999,999,999
                long min = 100000000000;
                long max = 999999999999;
                // Cryptographic source, so invoice numbers cannot be predicted from earlier ones.
                var offset = BitConverter.ToUInt64(System.Security.Cryptography.RandomNumberGenerator.GetBytes(8)) % (ulong)(max - min);
                code = (min + (long)offset).ToString();

                // Verify uniqueness in the payments table against the string column
                var exists = await _context.Payments
                    .AnyAsync(p => p.InvoiceId == code);

                if (!exists) isUnique = true;
            }

            return code;
        }
    }
}