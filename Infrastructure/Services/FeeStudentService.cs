using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Abstractions.Services;
using Application.ViewModels.Fees;
using Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class FeeStudentService : IFeeStudentService
{
    private readonly BonzerSchoolDbContext _context;

    public FeeStudentService(BonzerSchoolDbContext context)
    {
        _context = context;
    }

    public async Task<StudentFeeDashboardDto> GetStudentDashboardByRoll(Guid branchId, Guid classId, string rollNumber, Guid academicYearId)
    {
        if (string.IsNullOrWhiteSpace(rollNumber))
        {
            throw new ArgumentException("Roll number is required.", nameof(rollNumber));
        }

        var student = await _context.students
            .AsNoTracking()
            .FirstOrDefaultAsync(s =>
                s.BranchId == branchId &&
                s.ClassId == classId &&
                s.RollNumber == rollNumber);

        if (student == null)
        {
            throw new InvalidOperationException("Student not found for given branch, class and roll number.");
        }

        return await GetStudentDashboard(student.Id, academicYearId);
    }

    public async Task<StudentFeeDashboardDto> GetStudentDashboard(Guid studentId, Guid academicYearId)
    {
        var academicYear = await _context.academic_years
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == academicYearId);

        if (academicYear == null || academicYear.StartDate == null || academicYear.EndDate == null)
        {
            throw new InvalidOperationException("Invalid academic year.");
        }

        var start = academicYear.StartDate.Value;
        var end = academicYear.EndDate.Value;

        var itemQuery = _context.invoice_items
            .Join(
                _context.invoices,
                item => item.InvoiceId,
                inv => inv.Id,
                (item, inv) => new { item, inv })
            .Where(x =>
                x.inv.StudentId == studentId &&
                x.inv.InvoiceDate.HasValue &&
                x.inv.InvoiceDate.Value >= start &&
                x.inv.InvoiceDate.Value <= end);

        var itemList = await itemQuery.ToListAsync();

        decimal totalFees;
        decimal paidFees;
        decimal remainingFees;
        List<StudentInstallmentDto> installmentBreakdown;

        if (itemList.Count > 0)
        {
            totalFees = itemList.Sum(x => x.item.TotalAmount);
            paidFees = itemList.Sum(x => x.item.PaidAmount);
            remainingFees = itemList.Sum(x => x.item.RemainingAmount);

            installmentBreakdown = await _context.fee_installments
                .Join(
                    itemList,
                    fi => fi.Id,
                    x => x.item.InstallmentId,
                    (fi, x) => new { fi, x.item })
                .GroupBy(x => new { x.fi.Id, x.fi.InstallmentName, x.fi.DueDate })
                .Select(g => new StudentInstallmentDto
                {
                    InstallmentId = g.Key.Id,
                    InstallmentName = g.Key.InstallmentName ?? string.Empty,
                    TotalAmount = g.Sum(y => y.item.TotalAmount),
                    PaidAmount = g.Sum(y => y.item.PaidAmount),
                    RemainingAmount = g.Sum(y => y.item.RemainingAmount),
                    DueDate = g.Key.DueDate.HasValue
                        ? new DateTime(g.Key.DueDate.Value.Year, g.Key.DueDate.Value.Month, g.Key.DueDate.Value.Day)
                        : null
                })
                .ToListAsync();
        }
        else
        {
            var invoices = await _context.invoices
                .Where(i => i.StudentId == studentId
                            && i.InvoiceDate.HasValue
                            && i.InvoiceDate.Value >= start
                            && i.InvoiceDate.Value <= end)
                .ToListAsync();

            var payments = await _context.payments
                .Where(p => p.StudentId == studentId
                            && p.PaymentDate.HasValue
                            && p.PaymentDate.Value >= start
                            && p.PaymentDate.Value <= end)
                .ToListAsync();

            totalFees = invoices.Sum(x => x.TotalAmount ?? 0m);
            paidFees = payments.Sum(x => x.Amount ?? 0m);
            remainingFees = totalFees - paidFees;

            installmentBreakdown = await _context.fee_installments
                .Join(
                    invoices,
                    fi => fi.Id,
                    inv => inv.InstallmentId,
                    (fi, inv) => new { fi, inv })
                .GroupBy(x => new { x.fi.Id, x.fi.InstallmentName, x.fi.DueDate })
                .Select(g => new StudentInstallmentDto
                {
                    InstallmentId = g.Key.Id,
                    InstallmentName = g.Key.InstallmentName ?? string.Empty,
                    TotalAmount = g.Sum(x => x.inv.TotalAmount ?? 0m),
                    PaidAmount = g.Sum(x => x.inv.PaidAmount ?? 0m),
                    RemainingAmount = g.Sum(x => x.inv.RemainingAmount ?? 0m),
                    DueDate = g.Key.DueDate.HasValue
                        ? new DateTime(g.Key.DueDate.Value.Year, g.Key.DueDate.Value.Month, g.Key.DueDate.Value.Day)
                        : null
                })
                .ToListAsync();
        }

        // Fee head level paid/remaining is not directly derivable from current schema,
        // so we only compute scheduled totals per head from class fee mappings.
        var student = await _context.students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == studentId);

        var feeHeads = new List<StudentFeeHeadDto>();

        if (student != null && student.ClassId.HasValue)
        {
            if (itemList.Count > 0)
            {
                feeHeads = await _context.fee_heads
                    .Join(
                        itemList,
                        fh => fh.Id,
                        x => x.item.FeeHeadId,
                        (fh, x) => new { fh, x.item })
                    .GroupBy(x => new { x.fh.Id, x.fh.Name })
                    .Select(g => new StudentFeeHeadDto
                    {
                        FeeHeadId = g.Key.Id,
                        FeeHeadName = g.Key.Name ?? string.Empty,
                        TotalAmount = g.Sum(y => y.item.TotalAmount),
                        PaidAmount = g.Sum(y => y.item.PaidAmount),
                        RemainingAmount = g.Sum(y => y.item.RemainingAmount)
                    })
                    .ToListAsync();
            }
            else
            {
                var classFeeMaps = await _context.class_fee_maps
                    .Where(cfm =>
                        cfm.ClassId == student.ClassId
                        && cfm.AcademicYearId == academicYearId
                        && (cfm.DivisionId == null || cfm.DivisionId == student.DivisionId))
                    .ToListAsync();

                feeHeads = await _context.fee_heads
                    .Join(
                        classFeeMaps,
                        fh => fh.Id,
                        cfm => cfm.FeeHeadId,
                        (fh, cfm) => new { fh, cfm })
                    .GroupBy(x => new { x.fh.Id, x.fh.Name })
                    .Select(g => new StudentFeeHeadDto
                    {
                        FeeHeadId = g.Key.Id,
                        FeeHeadName = g.Key.Name ?? string.Empty,
                        TotalAmount = g.Sum(x => x.cfm.Amount ?? 0m),
                        PaidAmount = 0m,
                        RemainingAmount = g.Sum(x => x.cfm.Amount ?? 0m),
                    })
                    .ToListAsync();

                // Distribute actual paid/remaining proportionally across fee heads for analytics
                var totalHeadAmount = feeHeads.Sum(f => f.TotalAmount);
                if (totalHeadAmount > 0 && totalFees > 0)
                {
                    foreach (var head in feeHeads)
                    {
                        var share = head.TotalAmount / totalHeadAmount;
                        var allocatedPaid = Math.Round(paidFees * share, 2);
                        head.PaidAmount = allocatedPaid;
                        head.RemainingAmount = Math.Round(head.TotalAmount - allocatedPaid, 2);
                    }
                }
            }
        }

        return new StudentFeeDashboardDto
        {
            StudentId = studentId,
            RollNumber = student?.RollNumber,
            TotalFees = totalFees,
            PaidFees = paidFees,
            RemainingFees = remainingFees,
            FeeHeads = feeHeads,
            Installments = installmentBreakdown
        };
    }
}

