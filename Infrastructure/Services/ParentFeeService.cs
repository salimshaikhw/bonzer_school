using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Abstractions.Services;
using Application.Exceptions;
using Application.ViewModels.Fees;
using Application.ViewModels.Invoice;
using Application.ViewModels.Payment;
using Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class ParentFeeService : IParentFeeService
{
    private readonly BonzerSchoolDbContext _db;
    private readonly IPaymentService _payments;
    private readonly IInvoiceService _invoices;
    private readonly ILogger<ParentFeeService> _logger;

    private static readonly HashSet<string> StaffRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Admin", "Principal", "ClassTeacher", "User",
    };

    public ParentFeeService(
        BonzerSchoolDbContext db,
        IPaymentService payments,
        IInvoiceService invoices,
        ILogger<ParentFeeService> logger)
    {
        _db = db;
        _payments = payments;
        _invoices = invoices;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ParentLinkedStudentDto>> GetMyStudentsAsync(
        string? parentEmail,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(parentEmail))
            return Array.Empty<ParentLinkedStudentDto>();

        var email = parentEmail.Trim().ToLowerInvariant();
        var q =
            from p in _db.parent_users.AsNoTracking()
            join s in _db.students.AsNoTracking() on p.StudentId equals s.Id
            where p.StudentId != null
                  && p.Username != null
                  && p.Username.Trim().ToLower() == email
            select new ParentLinkedStudentDto
            {
                StudentId = s.Id,
                FirstName = s.FirstName,
                LastName = s.LastName,
                AdmissionNo = s.AdmissionNo,
                RollNumber = s.RollNumber,
                BranchId = s.BranchId,
                ClassId = s.ClassId,
                AcademicYearId = s.AcademicYearId,
            };

        return await q
            .OrderBy(x => x.AdmissionNo)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> CanAccessStudentForFeesAsync(
        Guid studentId,
        string? userEmail,
        IReadOnlyList<string> roleNames,
        CancellationToken cancellationToken = default)
    {
        if (roleNames.Any(r => StaffRoles.Contains(r.Trim())))
            return Task.FromResult(true);

        if (roleNames.Any(r => string.Equals(r, "Parent", StringComparison.OrdinalIgnoreCase))
            && !string.IsNullOrWhiteSpace(userEmail))
        {
            var email = userEmail.Trim().ToLowerInvariant();
            return _db.parent_users.AsNoTracking().AnyAsync(
                p => p.StudentId == studentId
                     && p.Username != null
                     && p.Username.Trim().ToLower() == email,
                cancellationToken);
        }

        return Task.FromResult(false);
    }

    public async Task<IReadOnlyList<OpenInvoiceDto>> GetOpenInvoicesAsync(
        Guid studentId,
        Guid academicYearId,
        string? userEmail,
        IReadOnlyList<string> roleNames,
        CancellationToken cancellationToken = default)
    {
        if (!await CanAccessStudentForFeesAsync(studentId, userEmail, roleNames, cancellationToken).ConfigureAwait(false))
            throw new BaseServiceException("Not allowed to view this student’s fees.", null, Application.Common.ExceptionCodes.UnAuthorized);

        var ay = await _db.academic_years.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == academicYearId, cancellationToken)
            .ConfigureAwait(false);
        if (ay?.StartDate == null || ay.EndDate == null)
            throw new BaseServiceException("Invalid academic year.", null, Application.Common.ExceptionCodes.Validation);

        var start = ay.StartDate.Value;
        var end = ay.EndDate.Value;

        var invoices = await _db.invoices.AsNoTracking()
            .Where(i =>
                i.StudentId == studentId
                && i.InvoiceDate.HasValue
                && i.InvoiceDate.Value >= start
                && i.InvoiceDate.Value <= end
                && (i.RemainingAmount ?? 0) > 0)
            .OrderBy(i => i.DueDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (invoices.Count == 0)
            return Array.Empty<OpenInvoiceDto>();

        var invIds = invoices.Select(i => i.Id).ToList();
        var items = await _db.invoice_items.AsNoTracking()
            .Where(ii => invIds.Contains(ii.InvoiceId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var headIds = items.Select(ii => ii.FeeHeadId).Distinct().ToList();
        var headRows = await _db.fee_heads.AsNoTracking()
            .Where(fh => headIds.Contains(fh.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var heads = headRows.ToDictionary(fh => fh.Id, fh => fh.Name ?? "");

        var result = new List<OpenInvoiceDto>();
        foreach (var inv in invoices)
        {
            var lines = items
                .Where(ii => ii.InvoiceId == inv.Id && ii.RemainingAmount > 0)
                .Select(ii => new OpenInvoiceLineDto
                {
                    InvoiceItemId = ii.Id,
                    FeeHeadId = ii.FeeHeadId,
                    FeeHeadName = heads.GetValueOrDefault(ii.FeeHeadId, ""),
                    TotalAmount = ii.TotalAmount,
                    PaidAmount = ii.PaidAmount,
                    RemainingAmount = ii.RemainingAmount,
                })
                .ToList();

            result.Add(new OpenInvoiceDto
            {
                Id = inv.Id,
                InvoiceNo = inv.InvoiceNo,
                DueDate = inv.DueDate,
                TotalAmount = inv.TotalAmount ?? 0,
                PaidAmount = inv.PaidAmount ?? 0,
                RemainingAmount = inv.RemainingAmount ?? 0,
                Lines = lines,
            });
        }

        return result;
    }

    public async Task<IReadOnlyList<PaymentHistoryRowDto>> GetPaymentHistoryAsync(
        Guid studentId,
        Guid academicYearId,
        string? userEmail,
        IReadOnlyList<string> roleNames,
        CancellationToken cancellationToken = default)
    {
        if (!await CanAccessStudentForFeesAsync(studentId, userEmail, roleNames, cancellationToken).ConfigureAwait(false))
            throw new BaseServiceException("Not allowed to view this student’s payments.", null, Application.Common.ExceptionCodes.UnAuthorized);

        var ay = await _db.academic_years.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == academicYearId, cancellationToken)
            .ConfigureAwait(false);
        if (ay?.StartDate == null || ay.EndDate == null)
            throw new BaseServiceException("Invalid academic year.", null, Application.Common.ExceptionCodes.Validation);

        var start = ay.StartDate.Value;
        var end = ay.EndDate.Value;

        return await _db.payments.AsNoTracking()
            .Where(p =>
                p.StudentId == studentId
                && p.PaymentDate.HasValue
                && p.PaymentDate.Value >= start
                && p.PaymentDate.Value <= end)
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.CreatedAt)
            .Select(p => new PaymentHistoryRowDto
            {
                PaymentId = p.Id,
                InvoiceId = p.InvoiceId,
                PaymentDate = p.PaymentDate,
                Amount = p.Amount,
                PaymentMethod = p.PaymentMethod,
                TransactionNumber = p.TransactionNumber,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<PaymentResponse?> RecordParentPaymentAsync(
        Guid studentId,
        ParentPayRequest request,
        string? userEmail,
        IReadOnlyList<string> roleNames,
        CancellationToken cancellationToken = default)
    {
        if (!await CanAccessStudentForFeesAsync(studentId, userEmail, roleNames, cancellationToken).ConfigureAwait(false))
            throw new BaseServiceException("Not allowed to pay for this student.", null, Application.Common.ExceptionCodes.UnAuthorized);

        if (request.FeeLines == null || request.FeeLines.Count == 0)
            throw new BaseServiceException("feeLines is required with at least one line.", null, Application.Common.ExceptionCodes.Validation);

        foreach (var line in request.FeeLines)
        {
            if (line.Amount <= 0)
                throw new BaseServiceException("Each fee line amount must be greater than zero.", null, Application.Common.ExceptionCodes.Validation);

            var hasItem = line.InvoiceItemId.HasValue;
            var hasHead = line.FeeHeadId.HasValue;
            var hasCustom = !string.IsNullOrWhiteSpace(line.CustomFeeName);
            if (!hasItem && !hasHead && !hasCustom)
                throw new BaseServiceException(
                    "Each fee line needs invoiceItemId, feeHeadId, or customFeeName.",
                    null,
                    Application.Common.ExceptionCodes.Validation);
        }

        var ay = await _db.academic_years.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AcademicYearId, cancellationToken)
            .ConfigureAwait(false);
        if (ay?.StartDate == null || ay.EndDate == null)
            throw new BaseServiceException("Invalid academic year.", null, Application.Common.ExceptionCodes.Validation);

        var inv = await _db.invoices.FirstOrDefaultAsync(
             i => i.Id.ToString() == request.InvoiceId && i.StudentId == studentId,
             cancellationToken)
         .ConfigureAwait(false);
        if (inv == null)
            throw new BaseServiceException("Invoice not found for this student.", null, Application.Common.ExceptionCodes.ItemNotFound);

        if (inv.InvoiceDate.HasValue &&
            (inv.InvoiceDate.Value < ay.StartDate.Value || inv.InvoiceDate.Value > ay.EndDate.Value))
        {
            throw new BaseServiceException(
                "Invoice date is outside the selected academic year.",
                null,
                Application.Common.ExceptionCodes.Validation);
        }

        var total = request.FeeLines.Sum(l => l.Amount);
        var remaining = inv.RemainingAmount ?? 0;
        if (total > remaining + 0.01m)
            throw new BaseServiceException("Sum of fee lines exceeds invoice balance.", null, Application.Common.ExceptionCodes.Validation);

        var student = await _db.students.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == studentId, cancellationToken)
            .ConfigureAwait(false);
        if (student == null)
            throw new BaseServiceException("Student not found.", null, Application.Common.ExceptionCodes.ItemNotFound);

        var feeLines = request.FeeLines
            .Select(f => new PaymentFeeLineRequest
            {
                InvoiceItemId = f.InvoiceItemId,
                FeeHeadId = f.FeeHeadId,
                CustomFeeName = string.IsNullOrWhiteSpace(f.CustomFeeName) ? null : f.CustomFeeName.Trim(),
                Amount = f.Amount,
            })
            .ToArray();

        var payReq = new PaymentRequest
        {
            InvoiceId = inv.Id.ToString(),
            StudentId = studentId,
            BranchId = inv.BranchId ?? student.BranchId,
            DivisionId = inv.DivisionId ?? student.DivisionId,
            CollectedByStaffId = null,
            PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Amount = total,
            PaymentMethod = string.IsNullOrWhiteSpace(request.PaymentMethod) ? "Online" : request.PaymentMethod!.Trim(),
            PaymentType = "Fee",
            TransactionNumber = string.IsNullOrWhiteSpace(request.TransactionReference)
                ? null
                : request.TransactionReference.Trim(),
            Remarks = roleNames.Any(r => string.Equals(r, "Parent", StringComparison.OrdinalIgnoreCase))
                ? "Parent portal payment"
                : "Recorded via fees portal",
            FeeLines = feeLines,
        };

        _logger.LogInformation(
            "ParentFee: recording payment StudentId={StudentId} InvoiceId={InvoiceId} Total={Total}",
            studentId,
            request.InvoiceId,
            total);

        return await _payments.CreateAsync(payReq).ConfigureAwait(false);
    }

    public async Task<PaymentResponse?> CollectAndPayAsync(
        Guid studentId,
        ParentCollectPayRequest request,
        string? userEmail,
        IReadOnlyList<string> roleNames,
        CancellationToken cancellationToken = default)
    {
        if (!await CanAccessStudentForFeesAsync(studentId, userEmail, roleNames, cancellationToken).ConfigureAwait(false))
            throw new BaseServiceException("Not allowed to pay for this student.", null, Application.Common.ExceptionCodes.UnAuthorized);

        if (request.FeeLines == null || request.FeeLines.Count == 0)
            throw new BaseServiceException("feeLines is required with at least one line.", null, Application.Common.ExceptionCodes.Validation);

        foreach (var line in request.FeeLines)
        {
            if (line.Amount <= 0)
                throw new BaseServiceException("Each fee line amount must be greater than zero.", null, Application.Common.ExceptionCodes.Validation);

            var hasItem = line.InvoiceItemId.HasValue;
            var hasHead = line.FeeHeadId.HasValue;
            var hasCustom = !string.IsNullOrWhiteSpace(line.CustomFeeName);
            if (!hasItem && !hasHead && !hasCustom)
                throw new BaseServiceException(
                    "Each fee line needs invoiceItemId, feeHeadId, or customFeeName.",
                    null,
                    Application.Common.ExceptionCodes.Validation);
        }

        var ay = await _db.academic_years.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AcademicYearId, cancellationToken)
            .ConfigureAwait(false);
        if (ay?.StartDate == null || ay.EndDate == null)
            throw new BaseServiceException("Invalid academic year.", null, Application.Common.ExceptionCodes.Validation);

        var start = ay.StartDate.Value;
        var end = ay.EndDate.Value;

        Guid invoiceId;
        if (request.InvoiceId.HasValue)
        {
            var invCheck = await _db.invoices.AsNoTracking()
                .FirstOrDefaultAsync(
                    i => i.Id == request.InvoiceId.Value && i.StudentId == studentId,
                    cancellationToken)
                .ConfigureAwait(false);
            if (invCheck == null)
                throw new BaseServiceException("Invoice not found for this student.", null, Application.Common.ExceptionCodes.ItemNotFound);
            if (invCheck.InvoiceDate.HasValue &&
                (invCheck.InvoiceDate.Value < start || invCheck.InvoiceDate.Value > end))
            {
                throw new BaseServiceException(
                    "Invoice date is outside the selected academic year.",
                    null,
                    Application.Common.ExceptionCodes.Validation);
            }

            invoiceId = request.InvoiceId.Value;
        }
        else
        {
            var open = await _db.invoices.AsNoTracking()
                .Where(i =>
                    i.StudentId == studentId
                    && i.InvoiceDate.HasValue
                    && i.InvoiceDate.Value >= start
                    && i.InvoiceDate.Value <= end
                    && (i.RemainingAmount ?? 0) > 0)
                .OrderBy(i => i.DueDate)
                .Select(i => i.Id)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (open != Guid.Empty)
            {
                invoiceId = open;
            }
            else
            {
                var created = await _invoices.CreateFromStudentScheduleAsync(
                        new CreateInvoiceFromScheduleRequest
                        {
                            StudentId = studentId,
                            AcademicYearId = request.AcademicYearId,
                            InstallmentId = request.InstallmentId,
                        })
                    .ConfigureAwait(false);
                invoiceId = created.Id;
            }
        }

        var payReq = new ParentPayRequest
        {
            InvoiceId = invoiceId.ToString(),
            AcademicYearId = request.AcademicYearId,
            PaymentMethod = request.PaymentMethod,
            TransactionReference = request.TransactionReference,
            FeeLines = request.FeeLines,
        };

        _logger.LogInformation(
            "ParentFee: collect-and-pay StudentId={StudentId} InvoiceId={InvoiceId}",
            studentId,
            invoiceId);

        return await RecordParentPaymentAsync(studentId, payReq, userEmail, roleNames, cancellationToken)
            .ConfigureAwait(false);
    }
}
