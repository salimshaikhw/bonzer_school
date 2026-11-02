using Domain.Entities.Students;
using Application.Fees;
using Application.Abstractions;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common;
using Application.Exceptions;
using Application.ViewModels.Invoice;
using AutoMapper;
using Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Infrastructure;

namespace Infrastructure.Services.Fees
{
    public class InvoiceService : IInvoiceService
    {
        private readonly ILogger<InvoiceService> _logger;
        private readonly IInvoiceRepository _invoiceRepo;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly BonzerSchoolDbContext _context;
        private readonly IFeeStudentService _feeStudentService;

        public InvoiceService(
            ILogger<InvoiceService> logger,
            IInvoiceRepository invoiceRepo,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICacheService cacheService,
            BonzerSchoolDbContext context,
            IFeeStudentService feeStudentService)
        {
            _logger = logger;
            _invoiceRepo = invoiceRepo;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
            _context = context;
            _feeStudentService = feeStudentService;
        }

        public async Task<IEnumerable<InvoiceResponse>> GetAllAsync()
        {
            _logger.LogDebug("Fetching all Invoices");
            var entities = await _invoiceRepo.GetAllAsync();
            return _mapper.Map<IEnumerable<InvoiceResponse>>(entities);
        }

        public async Task<InvoiceResponse> GetByIdAsync(Guid id)
        {
            _logger.LogDebug("Fetching Invoice by id: {Id}", id);
            var entity = await _invoiceRepo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<InvoiceResponse>(entity);
        }

        /// <summary>
        /// Manual invoice: the total is split over the fee heads the student's class owes for the academic year of
        /// the invoice date (their division's fees included), and the invoice is saved together with its items.
        /// Paid / remaining / status always start from the items, whatever the request says.
        /// </summary>
        public async Task<InvoiceResponse> CreateAsync(InvoiceRequest request)
        {
            var entity = _mapper.Map<Invoice>(request);
            entity.Id = entity.Id == Guid.Empty ? Guid.NewGuid() : entity.Id;
            if ((entity.TotalAmount ?? 0) <= 0)
                throw new BaseServiceException("The invoice total must be greater than zero.", null, ExceptionCodes.Validation);

            var items = await BuildInvoiceItemsAsync(entity);
            InvoiceSettlement.Recalculate(entity, items);
            _context.Invoices.Add(entity);
            _context.InvoiceItems.AddRange(items);
            await _context.SaveChangesAsync();

            _cacheService.Remove("Invoices:all");
            return _mapper.Map<InvoiceResponse>(entity);
        }

        /// <inheritdoc />
        public async Task<InvoiceResponse> CreateFromStudentScheduleAsync(CreateInvoiceFromScheduleRequest request)
        {
            if (request == null)
                throw new BaseServiceException("Request required.", null, ExceptionCodes.Validation);

            var studentId = request.StudentId;
            var academicYearId = request.AcademicYearId;

            var ay = await _context.AcademicYears.AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == academicYearId)
                .ConfigureAwait(false);
            if (ay?.StartDate == null || ay.EndDate == null)
                throw new BaseServiceException("Invalid academic year.", null, ExceptionCodes.Validation);

            var start = ay.StartDate.Value;
            var end = ay.EndDate.Value;

            var hasOpen = await _context.Invoices.AsNoTracking()
                .AnyAsync(i =>
                    i.StudentId == studentId
                    && i.InvoiceDate.HasValue
                    && i.InvoiceDate.Value >= start
                    && i.InvoiceDate.Value <= end
                    && (i.RemainingAmount ?? 0) > 0)
                .ConfigureAwait(false);
            if (hasOpen)
                throw new BaseServiceException(
                    "This student already has an open invoice for this academic year. Pay against that invoice or clear it first.",
                    null,
                    ExceptionCodes.Validation);

            var student = await _context.Students.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == studentId)
                .ConfigureAwait(false);
            if (student == null)
                throw new BaseServiceException("Student not found.", null, ExceptionCodes.ItemNotFound);
            if (!student.BranchId.HasValue || !student.ClassId.HasValue)
                throw new BaseServiceException("Student must have branch and class set to create a fee invoice.", null, ExceptionCodes.Validation);

            var dashboard = await _feeStudentService.GetStudentDashboard(studentId, academicYearId)
                .ConfigureAwait(false);

            var heads = dashboard.FeeHeads
                .Where(h => h.RemainingAmount > 0.009m)
                .ToList();
            if (heads.Count == 0)
                throw new BaseServiceException(
                    "No outstanding fee heads for this student in the fee schedule. Check class fee mapping and academic year.",
                    null,
                    ExceptionCodes.Validation);

            var total = heads.Sum(h => h.RemainingAmount);
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (today < start) today = start;
            if (today > end) today = end;

            var invoiceId = Guid.NewGuid();
            var installmentId = request.InstallmentId;

            var entity = new Invoice
            {
                Id = invoiceId,
                StudentId = studentId,
                BranchId = student.BranchId,
                ClassId = student.ClassId,
                DivisionId = student.DivisionId,
                InstallmentId = installmentId,
                InvoiceNo = $"SCH-{today:yyyyMMdd}-{invoiceId.ToString("N")[..8].ToUpperInvariant()}",
                InvoiceDate = today,
                TotalAmount = total,
                PaidAmount = 0,
                RemainingAmount = total,
                Status = "Open",
                CreatedAt = DateTime.UtcNow,
            };


            var items = heads.Select(h => new InvoiceItem
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoiceId,
                FeeHeadId = h.FeeHeadId,
                InstallmentId = installmentId,
                TotalAmount = h.RemainingAmount,
                PaidAmount = 0,
                RemainingAmount = h.RemainingAmount,
                CreatedAt = DateTime.UtcNow,
            }).ToList();

            // Invoice and items in one transaction: never an invoice without its breakdown.
            _context.Invoices.Add(entity);
            _context.InvoiceItems.AddRange(items);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            _cacheService.Remove("Invoices:all");

            var persisted = await _invoiceRepo.GetByIdAsync(invoiceId).ConfigureAwait(false);
            return _mapper.Map<InvoiceResponse>(persisted!);
        }

        /// <summary>
        /// Updates the descriptive fields of an invoice (number, dates, month / year, installment). The amounts and
        /// status follow its items and payments (InvoiceSettlement) and cannot be set directly; the student and the
        /// total cannot change once anything was paid.
        /// </summary>
        public async Task<InvoiceResponse> UpdateAsync(Guid id, InvoiceRequest request)
        {
            var entity = await _invoiceRepo.GetByIdAsync(id);
            var items = await _context.InvoiceItems.Where(i => i.InvoiceId == id).ToListAsync();
            var paid = items.Sum(i => i.PaidAmount);
            if (paid > 0 && ((request.StudentId != null && request.StudentId != entity.StudentId)
                             || (request.TotalAmount is { } total && total != entity.TotalAmount)))
                throw new BaseServiceException(
                    "Payments were already made against this invoice; its student and total can no longer change.", null, ExceptionCodes.Operation);

            var (studentId, totalAmount) = (entity.StudentId, entity.TotalAmount);
            _mapper.Map(request, entity);
            entity.Id = id;
            if (paid > 0)
                (entity.StudentId, entity.TotalAmount) = (studentId, totalAmount);
            if (items.Count > 0)
                InvoiceSettlement.Recalculate(entity, items);
            else
                (entity.PaidAmount, entity.RemainingAmount, entity.Status) = (0, entity.TotalAmount, InvoiceStatuses.Open);

            await _invoiceRepo.UpdateAsync(entity);
            _cacheService.Remove("Invoices:all");
            _cacheService.Remove($"Invoice:{id}");
            return _mapper.Map<InvoiceResponse>(entity);
        }

        /// <summary>
        /// Splits the invoice total over the fee heads the student owes (FeeLedger rules: class + division fees of
        /// the academic year containing the invoice date), in proportion to their amounts; the last item takes
        /// the rounding difference so the items always add up to the total. No applicable fees: one item cannot be
        /// derived, so the invoice is refused.
        /// </summary>
        private async Task<List<InvoiceItem>> BuildInvoiceItemsAsync(Invoice invoice)
        {
            var student = invoice.StudentId is { } studentId
                ? await _context.Students.AsNoTracking().FirstOrDefaultAsync(st => st.Id == studentId)
                : null;
            if (student == null)
                throw new BaseServiceException("An invoice needs an existing student.", null, ExceptionCodes.Validation);

            var date = invoice.InvoiceDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var year = await _context.AcademicYears.AsNoTracking()
                .FirstOrDefaultAsync(y => y.StartDate <= date && y.EndDate >= date);
            var scope = new Student { Id = student.Id, ClassId = invoice.ClassId ?? student.ClassId, DivisionId = invoice.DivisionId ?? student.DivisionId, AcademicYearId = year?.Id ?? student.AcademicYearId };
            var fees = (await _context.ClassFeeMaps.AsNoTracking().Where(f => f.ClassId == scope.ClassId).ToListAsync())
                .Where(f => FeeLedger.Applies(f, scope) && f.FeeHeadId != null && (f.Amount ?? 0) > 0)
                .ToList();
            if (fees.Count == 0)
                throw new BaseServiceException("No fee heads are set for this student's class and academic year.", null, ExceptionCodes.Validation);

            var invoiceTotal = invoice.TotalAmount ?? 0m;
            var configured = fees.Sum(f => f.Amount!.Value);
            var items = fees.Select(f => new InvoiceItem
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoice.Id,
                FeeHeadId = f.FeeHeadId!.Value,
                InstallmentId = invoice.InstallmentId,
                TotalAmount = Math.Round(invoiceTotal * f.Amount!.Value / configured, 2),
            }).ToList();
            items[^1].TotalAmount += invoiceTotal - items.Sum(i => i.TotalAmount);
            foreach (var item in items)
                item.RemainingAmount = item.TotalAmount;
            return items;
        }

        /// <inheritdoc />
        public async Task<InvoiceStatementDto?> GetInvoiceStatementAsync(Guid invoiceId)
        {
            var entity = await _invoiceRepo.GetByIdAsync(invoiceId).ConfigureAwait(false);
            if (entity == null)
                return null;

            var items = await _context.InvoiceItems
                .AsNoTracking()
                .Where(ii => ii.InvoiceId == invoiceId)
                .ToListAsync()
                .ConfigureAwait(false);

            var headIds = items.Select(i => i.FeeHeadId).Distinct().ToList();
            var headNames = headIds.Count == 0
                ? new Dictionary<Guid, string>()
                : await _context.FeeHeads
                    .AsNoTracking()
                    .Where(fh => headIds.Contains(fh.Id))
                    .ToDictionaryAsync(fh => fh.Id, fh => fh.Name ?? string.Empty)
                    .ConfigureAwait(false);

            var lines = items
                .Select(ii => new InvoiceStatementLineDto
                {
                    FeeHeadId = ii.FeeHeadId,
                    FeeHeadName = headNames.GetValueOrDefault(ii.FeeHeadId, string.Empty),
                    TotalAmount = ii.TotalAmount,
                    PaidAmount = ii.PaidAmount,
                    RemainingAmount = ii.RemainingAmount,
                })
                .ToList();

            var lastPay = await _context.Payments
                .AsNoTracking()
                .Where(p => p.InvoiceId == invoiceId.ToString())
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);

            InvoiceLastPaymentDto? lastDto = null;
            if (lastPay != null)
            {
                var pflRows = await _context.PaymentFeeLines
                    .AsNoTracking()
                    .Where(pfl => pfl.PaymentId == lastPay.Id)
                    .ToListAsync()
                    .ConfigureAwait(false);

                var pflHeadIds = pflRows.Where(r => r.FeeHeadId.HasValue).Select(r => r.FeeHeadId!.Value).Distinct().ToList();
                var pflHeadNames = pflHeadIds.Count == 0
                    ? new Dictionary<Guid, string>()
                    : await _context.FeeHeads
                        .AsNoTracking()
                        .Where(fh => pflHeadIds.Contains(fh.Id))
                        .ToDictionaryAsync(fh => fh.Id, fh => fh.Name ?? string.Empty)
                        .ConfigureAwait(false);

                var feeLines = pflRows
                    .Select(pfl =>
                    {
                        var label = pfl.FeeHeadId.HasValue
                            ? pflHeadNames.GetValueOrDefault(pfl.FeeHeadId.Value, string.Empty)
                            : pfl.CustomFeeName ?? "Other";
                        return new InvoiceLastPaymentLineDto
                        {
                            Particulars = label,
                            Amount = pfl.Amount,
                        };
                    })
                    .ToList();

                lastDto = new InvoiceLastPaymentDto
                {
                    PaymentId = lastPay.Id,
                    PaymentDate = lastPay.PaymentDate,
                    Amount = lastPay.Amount,
                    PaymentMethod = lastPay.PaymentMethod,
                    TransactionNumber = lastPay.TransactionNumber,
                    FeeLines = feeLines,
                };
            }

            return new InvoiceStatementDto
            {
                Invoice = _mapper.Map<InvoiceResponse>(entity),
                Lines = lines,
                LastPayment = lastDto,
            };
        }

        /// <summary>An invoice with payments against it cannot be deleted (delete the payments first); otherwise it goes with its items.</summary>
        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _invoiceRepo.GetByIdAsync(id);
            var items = await _context.InvoiceItems.Where(i => i.InvoiceId == id).ToListAsync();
            var itemIds = items.Select(i => i.Id).ToList();
            if (items.Any(i => i.PaidAmount > 0)
                || await _context.PaymentFeeLines.AnyAsync(l => l.InvoiceItemId != null && itemIds.Contains(l.InvoiceItemId.Value)))
                throw new BaseServiceException("Payments were made against this invoice; delete those payments first.", null, ExceptionCodes.Operation);

            _context.InvoiceItems.RemoveRange(items);
            _context.Invoices.Remove(entity);
            await _context.SaveChangesAsync();
            _cacheService.Remove("Invoices:all");
            _cacheService.Remove($"Invoice:{id}");
            return true;
        }

    }
}