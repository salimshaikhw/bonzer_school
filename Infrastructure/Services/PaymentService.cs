using Application.Abstractions;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common;
using Application.Exceptions;
using Application.ViewModels.Payment;
using AutoMapper;
using Domain.Entities.Postgres.Scaffolded;
using Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly ILogger<PaymentService> _logger;
        private readonly IPaymentRepository _paymentRepo;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly BonzerSchoolDbContext _context;

        public PaymentService(
            ILogger<PaymentService> logger,
            IPaymentRepository paymentRepo,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            BonzerSchoolDbContext context)
        {
            _logger = logger;
            _paymentRepo = paymentRepo;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _context = context;
        }

        public async Task<IEnumerable<PaymentResponse>> GetAllAsync()
        {
            _logger.LogInformation("Fetching all Payments");
            var entities = await _paymentRepo.GetAllAsync();
            return _mapper.Map<IEnumerable<PaymentResponse>>(entities);
        }

        public async Task<PaymentResponse> GetByIdAsync(Guid id)
        {
            _logger.LogInformation($"Fetching Payment by id: {id}");
            var entity = await _paymentRepo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<PaymentResponse>(entity);
        }

        public async Task<PaymentResponse> CreateAsync(PaymentRequest request)
        {
            _logger.LogInformation("Creating Payment with auto-generated 10-digit Invoice ID");

            // 1. Generate Unique 10-digit Invoice ID
            string generatedInvoiceId = await GenerateUniqueInvoiceNumberAsync();

            // 2. Validation & Auto-Sum for the Payment Amount
            if (request.FeeLines != null && request.FeeLines.Length > 0)
            {
                decimal sumLines = request.FeeLines.Sum(fl => fl.Amount ?? 0m);

                if (request.Amount.HasValue && Math.Abs(request.Amount.Value - sumLines) > 0.01m)
                {
                    throw new BaseServiceException(
                        "Payment amount must equal the sum of fee line amounts.",
                        null,
                        ExceptionCodes.Validation);
                }

                request.Amount ??= sumLines;
            }

            // 3. Map and Save the Main Payment Entity
            var entity = _mapper.Map<Payment>(request);

            // Assign the 10-digit string directly to the string property
            entity.InvoiceId = generatedInvoiceId;

            await _paymentRepo.AddAsync(entity);
            await _unitOfWork.Commit();

            // 4. Store the Fee Line Breakdown
            if (request.FeeLines != null && request.FeeLines.Length > 0)
            {
                foreach (var line in request.FeeLines)
                {
                    decimal safeAmount = line.Amount ?? 0m;

                    if (safeAmount <= 0)
                    {
                        throw new BaseServiceException(
                            "Each fee line amount must be greater than zero.",
                            null,
                            ExceptionCodes.Validation);
                    }

                    if (!line.FeeHeadId.HasValue && string.IsNullOrWhiteSpace(line.CustomFeeName))
                    {
                        throw new BaseServiceException(
                            "Each fee line needs a feeHeadId or a customFeeName.",
                            null,
                            ExceptionCodes.Validation);
                    }

                    _context.payment_fee_lines.Add(new PaymentFeeLine
                    {
                        Id = Guid.NewGuid(),
                        PaymentId = entity.Id,
                        FeeHeadId = line.FeeHeadId,
                        Amount = safeAmount,
                        CustomFeeName = string.IsNullOrWhiteSpace(line.CustomFeeName)
                            ? null
                            : line.CustomFeeName.Trim(),
                    });
                }

                await _context.SaveChangesAsync();
            }

            return _mapper.Map<PaymentResponse>(entity);
        }

        public async Task<PaymentResponse> UpdateAsync(Guid id, PaymentRequest request)
        {
            _logger.LogInformation($"Updating Payment id: {id}");
            var entity = await _paymentRepo.GetByIdAsync(id);
            if (entity == null) return null;

            _mapper.Map(request, entity);
            await _paymentRepo.UpdateAsync(entity);
            await _unitOfWork.Commit();

            return _mapper.Map<PaymentResponse>(entity);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation($"Deleting Payment id: {id}");
            await _paymentRepo.DeleteAsync(id);
            await _unitOfWork.Commit();
            return true;
        }

        public async Task<PaymentReceiptDto?> GetReceiptAsync(Guid paymentId)
        {
            var payment = await _context.payments.FirstOrDefaultAsync(p => p.Id == paymentId);
            if (payment == null) return null;

            var student = payment.StudentId.HasValue
                ? await _context.students.FirstOrDefaultAsync(s => s.Id == payment.StudentId.Value)
                : null;

            var branch = payment.BranchId.HasValue
                ? await _context.branches.FirstOrDefaultAsync(b => b.Id == payment.BranchId.Value)
                : null;

            var pflRows = await _context.payment_fee_lines
                .Where(pfl => pfl.PaymentId == paymentId)
                .ToListAsync();

            var headIds = pflRows.Where(r => r.FeeHeadId.HasValue).Select(r => r.FeeHeadId!.Value).Distinct().ToList();

            var headNames = headIds.Count == 0
                ? new Dictionary<Guid, string>()
                : await _context.fee_heads.AsNoTracking()
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
            var random = new Random();
            string code = string.Empty;
            bool isUnique = false;

            while (!isUnique)
            {
                // Generates a 10-digit number string
                // Range: 100,000,000,000 to 999,999,999,999
                long min = 100000000000;
                long max = 999999999999;
                code = (random.NextInt64(min, max)).ToString();

                // Verify uniqueness in the payments table against the string column
                var exists = await _context.payments
                    .AnyAsync(p => p.InvoiceId == code);

                if (!exists) isUnique = true;
            }

            return code;
        }
    }
}