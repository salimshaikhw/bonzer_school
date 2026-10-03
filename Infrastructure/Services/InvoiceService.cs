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

namespace Infrastructure.Services
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
            _logger.LogInformation("Fetching all Invoices");
            var entities = await _invoiceRepo.GetAllAsync();
            return _mapper.Map<IEnumerable<InvoiceResponse>>(entities);
        }

        public async Task<InvoiceResponse> GetByIdAsync(Guid id)
        {
            _logger.LogInformation($"Fetching Invoice by id: {id}");
            var entity = await _invoiceRepo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<InvoiceResponse>(entity);
        }

        public async Task<InvoiceResponse> CreateAsync(InvoiceRequest request)
        {
            _logger.LogInformation("Creating Invoice");
            var entity = _mapper.Map<Invoice>(request);

            await _invoiceRepo.AddAsync(entity);
            await _unitOfWork.Commit();

             // create invoice_items per fee head for analytics and fee head–wise tracking
             await CreateInvoiceItemsForInvoice(entity);

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

            var ay = await _context.academic_years.AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == academicYearId)
                .ConfigureAwait(false);
            if (ay?.StartDate == null || ay.EndDate == null)
                throw new BaseServiceException("Invalid academic year.", null, ExceptionCodes.Validation);

            var start = ay.StartDate.Value;
            var end = ay.EndDate.Value;

            var hasOpen = await _context.invoices.AsNoTracking()
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

            var student = await _context.students.AsNoTracking()
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

            await _invoiceRepo.AddAsync(entity).ConfigureAwait(false);
            await _unitOfWork.Commit().ConfigureAwait(false);

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

            _context.invoice_items.AddRange(items);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            _cacheService.Remove("Invoices:all");

            var persisted = await _invoiceRepo.GetByIdAsync(invoiceId).ConfigureAwait(false);
            return _mapper.Map<InvoiceResponse>(persisted!);
        }

        public async Task<InvoiceResponse> UpdateAsync(Guid id, InvoiceRequest request)
        {
            _logger.LogInformation($"Updating Invoice id: {id}");
            var entity = await _invoiceRepo.GetByIdAsync(id);
            if (entity == null) return null;

            _mapper.Map(request, entity);
            await _invoiceRepo.UpdateAsync(entity);
            await _unitOfWork.Commit();

            _cacheService.Remove("Invoices:all");
            _cacheService.Remove($"Invoice:{id}");

            return _mapper.Map<InvoiceResponse>(entity);
        }

        private async Task CreateInvoiceItemsForInvoice(Invoice invoice)
        {
            // Require basic context to derive fee heads; adjust rules as needed.
            if (!invoice.ClassId.HasValue || !invoice.BranchId.HasValue || !invoice.InstallmentId.HasValue)
            {
                return;
            }

            // NOTE: if you have AcademicYearId on Invoice, include it in this filter.
            var classFeeMaps = await _context.class_fee_maps
                .Where(cfm => cfm.ClassId == invoice.ClassId)
                .ToListAsync();

            if (classFeeMaps.Count == 0)
            {
                return;
            }

            var totalConfigAmount = classFeeMaps.Sum(cfm => cfm.Amount ?? 0m);
            var invoiceTotal = invoice.TotalAmount ?? 0m;
            if (totalConfigAmount <= 0 || invoiceTotal <= 0)
            {
                return;
            }

            var items = new List<InvoiceItem>();

            foreach (var map in classFeeMaps.Where(m => m.FeeHeadId.HasValue && m.Amount.HasValue && m.Amount.Value > 0))
            {
                var share = (map.Amount ?? 0m) / totalConfigAmount;
                var itemTotal = Math.Round(invoiceTotal * share, 2);

                items.Add(new InvoiceItem
                {
                    InvoiceId = invoice.Id,
                    FeeHeadId = map.FeeHeadId!.Value,
                    InstallmentId = invoice.InstallmentId,
                    TotalAmount = itemTotal,
                    PaidAmount = 0m,
                    RemainingAmount = itemTotal
                });
            }

            if (items.Count == 0)
            {
                return;
            }

            _context.invoice_items.AddRange(items);
            await _context.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task<InvoiceStatementDto?> GetInvoiceStatementAsync(Guid invoiceId)
        {
            var entity = await _invoiceRepo.GetByIdAsync(invoiceId).ConfigureAwait(false);
            if (entity == null)
                return null;

            var items = await _context.invoice_items
                .AsNoTracking()
                .Where(ii => ii.InvoiceId == invoiceId)
                .ToListAsync()
                .ConfigureAwait(false);

            var headIds = items.Select(i => i.FeeHeadId).Distinct().ToList();
            var headNames = headIds.Count == 0
                ? new Dictionary<Guid, string>()
                : await _context.fee_heads
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

            var lastPay = await _context.payments
                .AsNoTracking()
                .Where(p => p.InvoiceId == invoiceId.ToString())
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);

            InvoiceLastPaymentDto? lastDto = null;
            if (lastPay != null)
            {
                var pflRows = await _context.payment_fee_lines
                    .AsNoTracking()
                    .Where(pfl => pfl.PaymentId == lastPay.Id)
                    .ToListAsync()
                    .ConfigureAwait(false);

                var pflHeadIds = pflRows.Where(r => r.FeeHeadId.HasValue).Select(r => r.FeeHeadId!.Value).Distinct().ToList();
                var pflHeadNames = pflHeadIds.Count == 0
                    ? new Dictionary<Guid, string>()
                    : await _context.fee_heads
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

        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation($"Deleting Invoice id: {id}");
            await _invoiceRepo.DeleteAsync(id);
            await _unitOfWork.Commit();

            _cacheService.Remove("Invoices:all");
            _cacheService.Remove($"Invoice:{id}");

            return true;
        }
    }
}