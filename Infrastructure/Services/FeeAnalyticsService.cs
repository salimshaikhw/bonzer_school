using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Abstractions.Services;
using Application.ViewModels.Fees;
using Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class FeeAnalyticsService : IFeeAnalyticsService
{
    private readonly BonzerSchoolDbContext _context;

    public FeeAnalyticsService(BonzerSchoolDbContext context)
    {
        _context = context;
    }

    public async Task<ClassFeeDashboardDto> GetClassDashboard(Guid branchId, Guid classId, Guid academicYearId)
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

        var invoices = await _context.invoices
            .Where(x => x.BranchId == branchId
                        && x.ClassId == classId
                        && x.InvoiceDate.HasValue
                        && x.InvoiceDate.Value >= start
                        && x.InvoiceDate.Value <= end)
            .ToListAsync();

        var studentCount = await _context.students
            .Where(s => s.BranchId == branchId && s.ClassId == classId)
            .CountAsync();

        var total = invoices.Sum(x => x.TotalAmount ?? 0m);
        var collected = invoices.Sum(x => x.PaidAmount ?? 0m);
        var pending = invoices.Sum(x => x.RemainingAmount ?? 0m);

        return new ClassFeeDashboardDto
        {
            ClassId = classId,
            TotalFees = total,
            CollectedFees = collected,
            PendingFees = pending,
            TotalStudents = studentCount,
            CollectionRate = total == 0 ? 0 : (collected / total) * 100
        };
    }

    public async Task<DivisionFeeDashboardDto> GetDivisionDashboard(Guid branchId, Guid divisionId, Guid academicYearId)
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

        var invoices = await _context.invoices
            .Where(x => x.BranchId == branchId
                        && x.DivisionId == divisionId
                        && x.InvoiceDate.HasValue
                        && x.InvoiceDate.Value >= start
                        && x.InvoiceDate.Value <= end)
            .ToListAsync();

        var studentCount = await _context.students
            .Where(s => s.BranchId == branchId && s.DivisionId == divisionId)
            .CountAsync();

        var total = invoices.Sum(x => x.TotalAmount ?? 0m);
        var collected = invoices.Sum(x => x.PaidAmount ?? 0m);
        var pending = invoices.Sum(x => x.RemainingAmount ?? 0m);

        return new DivisionFeeDashboardDto
        {
            DivisionId = divisionId,
            TotalFees = total,
            CollectedFees = collected,
            PendingFees = pending,
            TotalStudents = studentCount,
            CollectionRate = total == 0 ? 0 : (collected / total) * 100
        };
    }

    public async Task<BranchFeeDashboardDto> GetBranchDashboard(Guid branchId, Guid academicYearId)
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

        var invoices = await _context.invoices
            .Where(x => x.BranchId == branchId
                        && x.InvoiceDate.HasValue
                        && x.InvoiceDate.Value >= start
                        && x.InvoiceDate.Value <= end)
            .ToListAsync();

        var studentCount = await _context.students
            .Where(s => s.BranchId == branchId)
            .CountAsync();

        var total = invoices.Sum(x => x.TotalAmount ?? 0m);
        var collected = invoices.Sum(x => x.PaidAmount ?? 0m);
        var pending = invoices.Sum(x => x.RemainingAmount ?? 0m);

        return new BranchFeeDashboardDto
        {
            BranchId = branchId,
            TotalFees = total,
            CollectedFees = collected,
            PendingFees = pending,
            TotalStudents = studentCount,
            CollectionRate = total == 0 ? 0 : (collected / total) * 100
        };
    }

    public async Task<List<FeeCollectionTrendDto>> GetCollectionTrend(Guid branchId, DateTime startDate, DateTime endDate)
    {
        var start = DateOnly.FromDateTime(startDate);
        var end = DateOnly.FromDateTime(endDate);

        return await _context.payments
            .Where(p => p.BranchId == branchId
                        && p.PaymentDate.HasValue
                        && p.PaymentDate.Value >= start
                        && p.PaymentDate.Value <= end)
            .GroupBy(p => p.PaymentDate!.Value)
            .Select(g => new FeeCollectionTrendDto
            {
                Date = new DateTime(g.Key.Year, g.Key.Month, g.Key.Day),
                Amount = g.Sum(x => x.Amount ?? 0m)
            })
            .OrderBy(x => x.Date)
            .ToListAsync();
    }

    public async Task<List<FeeHeadAnalyticsDto>> GetFeeHeadAnalytics(Guid branchId, Guid academicYearId, Guid? classId, Guid? divisionId)
    {
        Guid? effectiveClassId = classId;
        if (!effectiveClassId.HasValue && divisionId.HasValue)
        {
            var divRow = await _context.divisions.AsNoTracking().FirstOrDefaultAsync(d => d.Id == divisionId.Value);
            effectiveClassId = divRow?.ClassId;
        }

        var classFeeMaps = await _context.class_fee_maps
            .Where(cfm => cfm.AcademicYearId == academicYearId)
            .ToListAsync();

        var classIds = classFeeMaps
            .Where(cfm => cfm.ClassId.HasValue)
            .Select(cfm => cfm.ClassId!.Value)
            .Distinct()
            .ToList();

        var classes = await _context.classes
            .Where(c => classIds.Contains(c.Id) && c.BranchId == branchId)
            .ToListAsync();

        var branchClassIds = classes.Select(c => c.Id).ToHashSet();

        var filteredMaps = classFeeMaps
            .Where(cfm => cfm.ClassId.HasValue && branchClassIds.Contains(cfm.ClassId.Value))
            .ToList();

        if (effectiveClassId.HasValue)
        {
            filteredMaps = filteredMaps
                .Where(cfm => cfm.ClassId == effectiveClassId.Value)
                .ToList();
        }

        var result = filteredMaps
            .GroupBy(cfm => cfm.FeeHeadId ?? Guid.Empty)
            .Select(g => new { FeeHeadId = g.Key, Amount = g.Sum(x => x.Amount ?? 0m) })
            .ToList();

        var headIds = result.Select(r => r.FeeHeadId).Where(id => id != Guid.Empty).Distinct().ToList();
        var heads = await _context.fee_heads
            .AsNoTracking()
            .Where(fh => headIds.Contains(fh.Id))
            .ToListAsync();

        return result
            .Where(x => x.FeeHeadId != Guid.Empty)
            .OrderByDescending(x => x.Amount)
            .Select(x => new FeeHeadAnalyticsDto
            {
                FeeHeadId = x.FeeHeadId,
                FeeHeadName = heads.FirstOrDefault(h => h.Id == x.FeeHeadId)?.Name ?? string.Empty,
                TotalAmount = x.Amount
            })
            .ToList();
    }

    public async Task<List<InstallmentCollectionDto>> GetInstallmentAnalytics(Guid branchId, Guid academicYearId, Guid? classId, Guid? divisionId)
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

        var query = _context.invoice_items
            .Join(
                _context.invoices,
                item => item.InvoiceId,
                inv => inv.Id,
                (item, inv) => new { item, inv })
            .Where(x =>
                x.inv.BranchId == branchId &&
                x.inv.InvoiceDate.HasValue &&
                x.inv.InvoiceDate.Value >= start &&
                x.inv.InvoiceDate.Value <= end);

        if (classId.HasValue)
        {
            query = query.Where(x => x.inv.ClassId == classId);
        }

        if (divisionId.HasValue)
        {
            query = query.Where(x => x.inv.DivisionId == divisionId);
        }

        var rows = await query.ToListAsync();
        var withInstallment = rows.Where(x => x.item.InstallmentId.HasValue).ToList();
        if (withInstallment.Count == 0)
        {
            return new List<InstallmentCollectionDto>();
        }

        var instIds = withInstallment.Select(x => x.item.InstallmentId!.Value).Distinct().ToList();
        var feeInstRows = await _context.fee_installments
            .AsNoTracking()
            .Where(fi => instIds.Contains(fi.Id))
            .ToListAsync();

        var branch = await _context.branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == branchId);

        Guid? classIdForLabel = classId;
        if (!classIdForLabel.HasValue && divisionId.HasValue)
        {
            var divForClass = await _context.divisions.AsNoTracking().FirstOrDefaultAsync(d => d.Id == divisionId.Value);
            classIdForLabel = divForClass?.ClassId;
        }

        string? className = null;
        if (classIdForLabel.HasValue)
        {
            var cls = await _context.classes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == classIdForLabel.Value);
            className = cls?.Name;
        }

        string? divisionName = null;
        if (divisionId.HasValue)
        {
            var div = await _context.divisions.AsNoTracking().FirstOrDefaultAsync(d => d.Id == divisionId.Value);
            divisionName = div?.Name;
        }

        var grouped = withInstallment
            .GroupBy(x => x.item.InstallmentId!.Value)
            .Select(g =>
            {
                var fi = feeInstRows.FirstOrDefault(r => r.Id == g.Key);
                var expected = g.Sum(x => x.item.TotalAmount);
                var collected = g.Sum(x => x.item.PaidAmount);
                var pending = expected - collected;
                var pct = expected > 0 ? Math.Round(collected / expected * 100m, 1) : 0m;
                return new InstallmentCollectionDto
                {
                    InstallmentId = g.Key,
                    InstallmentName = fi?.InstallmentName ?? string.Empty,
                    ExpectedAmount = expected,
                    CollectedAmount = collected,
                    PendingAmount = pending,
                    CollectionPercent = pct,
                    BranchName = branch?.Name,
                    ClassName = className,
                    DivisionName = divisionName,
                };
            })
            .OrderBy(x => feeInstRows.FirstOrDefault(f => f.Id == x.InstallmentId)?.InstallmentNo ?? 9999)
            .ThenBy(x => x.InstallmentName)
            .ToList();

        return grouped;
    }

    public async Task<List<StudentOutstandingFeeDto>> GetOutstandingByStudent(Guid branchId, Guid? classId, Guid? divisionId, Guid academicYearId)
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

        var itemsQuery = _context.invoice_items
            .Join(
                _context.invoices,
                item => item.InvoiceId,
                inv => inv.Id,
                (item, inv) => new { item, inv })
            .Where(x =>
                x.inv.BranchId == branchId &&
                x.inv.InvoiceDate.HasValue &&
                x.inv.InvoiceDate.Value >= start &&
                x.inv.InvoiceDate.Value <= end &&
                x.item.RemainingAmount > 0);

        if (classId.HasValue)
        {
            itemsQuery = itemsQuery.Where(x => x.inv.ClassId == classId);
        }

        if (divisionId.HasValue)
        {
            itemsQuery = itemsQuery.Where(x => x.inv.DivisionId == divisionId);
        }

        var items = await itemsQuery.ToListAsync();
        var studentIds = items
            .Where(x => x.inv.StudentId.HasValue)
            .Select(x => x.inv.StudentId!.Value)
            .Distinct()
            .ToList();

        var students = await _context.students
            .Where(s => studentIds.Contains(s.Id))
            .ToListAsync();

        var result = items
            .Where(x => x.inv.StudentId.HasValue)
            .GroupBy(x => x.inv.StudentId!.Value)
            .Select(g =>
            {
                var student = students.FirstOrDefault(s => s.Id == g.Key);

                return new StudentOutstandingFeeDto
                {
                    StudentId = g.Key,
                    RollNumber = student?.RollNumber,
                    ClassId = student?.ClassId,
                    DivisionId = student?.DivisionId,
                    RemainingAmount = g.Sum(x => x.item.RemainingAmount)
                };
            })
            .OrderByDescending(x => x.RemainingAmount)
            .ToList();

        return result;
    }

    public async Task<List<ClassOutstandingFeeDto>> GetOutstandingByClass(Guid branchId, Guid academicYearId)
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

        var items = await _context.invoice_items
            .Join(
                _context.invoices,
                item => item.InvoiceId,
                inv => inv.Id,
                (item, inv) => new { item, inv })
            .Where(x =>
                x.inv.BranchId == branchId &&
                x.inv.InvoiceDate.HasValue &&
                x.inv.InvoiceDate.Value >= start &&
                x.inv.InvoiceDate.Value <= end &&
                x.item.RemainingAmount > 0)
            .ToListAsync();

        var classIds = items
            .Where(x => x.inv.ClassId.HasValue)
            .Select(x => x.inv.ClassId!.Value)
            .Distinct()
            .ToList();

        var classes = await _context.classes
            .Where(c => classIds.Contains(c.Id))
            .ToListAsync();

        var result = items
            .Where(x => x.inv.ClassId.HasValue)
            .GroupBy(x => x.inv.ClassId!.Value)
            .Select(g =>
            {
                var cls = classes.FirstOrDefault(c => c.Id == g.Key);
                return new ClassOutstandingFeeDto
                {
                    ClassId = g.Key,
                    ClassName = cls?.Name ?? string.Empty,
                    RemainingAmount = g.Sum(x => x.item.RemainingAmount)
                };
            })
            .OrderByDescending(x => x.RemainingAmount)
            .ToList();

        return result;
    }

    public async Task<List<DivisionOutstandingFeeDto>> GetOutstandingByDivision(Guid branchId, Guid academicYearId)
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

        var items = await _context.invoice_items
            .Join(
                _context.invoices,
                item => item.InvoiceId,
                inv => inv.Id,
                (item, inv) => new { item, inv })
            .Where(x =>
                x.inv.BranchId == branchId &&
                x.inv.InvoiceDate.HasValue &&
                x.inv.InvoiceDate.Value >= start &&
                x.inv.InvoiceDate.Value <= end &&
                x.item.RemainingAmount > 0)
            .ToListAsync();

        var divisionIds = items
            .Where(x => x.inv.DivisionId.HasValue)
            .Select(x => x.inv.DivisionId!.Value)
            .Distinct()
            .ToList();

        var divisions = await _context.divisions
            .Where(d => divisionIds.Contains(d.Id))
            .ToListAsync();

        var result = items
            .Where(x => x.inv.DivisionId.HasValue)
            .GroupBy(x => x.inv.DivisionId!.Value)
            .Select(g =>
            {
                var div = divisions.FirstOrDefault(d => d.Id == g.Key);
                return new DivisionOutstandingFeeDto
                {
                    DivisionId = g.Key,
                    DivisionName = div?.Name ?? string.Empty,
                    RemainingAmount = g.Sum(x => x.item.RemainingAmount)
                };
            })
            .OrderByDescending(x => x.RemainingAmount)
            .ToList();

        return result;
    }

    public async Task<List<BranchOutstandingFeeDto>> GetOutstandingByBranch(Guid academicYearId)
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

        var items = await _context.invoice_items
            .Join(
                _context.invoices,
                item => item.InvoiceId,
                inv => inv.Id,
                (item, inv) => new { item, inv })
            .Where(x =>
                x.inv.BranchId.HasValue &&
                x.inv.InvoiceDate.HasValue &&
                x.inv.InvoiceDate.Value >= start &&
                x.inv.InvoiceDate.Value <= end &&
                x.item.RemainingAmount > 0)
            .ToListAsync();

        var branchIds = items
            .Where(x => x.inv.BranchId.HasValue)
            .Select(x => x.inv.BranchId!.Value)
            .Distinct()
            .ToList();

        var branches = await _context.branches
            .Where(b => branchIds.Contains(b.Id))
            .ToListAsync();

        var result = items
            .Where(x => x.inv.BranchId.HasValue)
            .GroupBy(x => x.inv.BranchId!.Value)
            .Select(g =>
            {
                var branch = branches.FirstOrDefault(b => b.Id == g.Key);
                return new BranchOutstandingFeeDto
                {
                    BranchId = g.Key,
                    BranchName = branch?.Name ?? string.Empty,
                    RemainingAmount = g.Sum(x => x.item.RemainingAmount)
                };
            })
            .OrderByDescending(x => x.RemainingAmount)
            .ToList();

        return result;
    }

    public async Task<List<StudentFeeHeadStatusDto>> GetFeeHeadStatus(
        Guid feeHeadId,
        Guid branchId,
        Guid academicYearId,
        Guid? classId,
        Guid? divisionId)
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

        var query = _context.invoice_items
            .Join(
                _context.invoices,
                item => item.InvoiceId,
                inv => inv.Id,
                (item, inv) => new { item, inv })
            .Where(x =>
                x.item.FeeHeadId == feeHeadId &&
                x.inv.BranchId == branchId &&
                x.inv.InvoiceDate.HasValue &&
                x.inv.InvoiceDate.Value >= start &&
                x.inv.InvoiceDate.Value <= end);

        if (classId.HasValue)
        {
            query = query.Where(x => x.inv.ClassId == classId);
        }

        if (divisionId.HasValue)
        {
            query = query.Where(x => x.inv.DivisionId == divisionId);
        }

        var items = await query.ToListAsync();

        if (items.Count == 0)
        {
            return new List<StudentFeeHeadStatusDto>();
        }

        var studentIds = items
            .Where(x => x.inv.StudentId.HasValue)
            .Select(x => x.inv.StudentId!.Value)
            .Distinct()
            .ToList();

        var students = await _context.students
            .Where(s => studentIds.Contains(s.Id))
            .ToListAsync();

        var feeHead = await _context.fee_heads
            .AsNoTracking()
            .FirstOrDefaultAsync(fh => fh.Id == feeHeadId);

        var feeHeadName = feeHead?.Name ?? string.Empty;

        var result = items
            .Where(x => x.inv.StudentId.HasValue)
            .GroupBy(x => x.inv.StudentId!.Value)
            .Select(g =>
            {
                var student = students.FirstOrDefault(s => s.Id == g.Key);

                return new StudentFeeHeadStatusDto
                {
                    StudentId = g.Key,
                    RollNumber = student?.RollNumber,
                    ClassId = student?.ClassId,
                    DivisionId = student?.DivisionId,
                    FeeHeadId = feeHeadId,
                    FeeHeadName = feeHeadName,
                    TotalAmount = g.Sum(x => x.item.TotalAmount),
                    PaidAmount = g.Sum(x => x.item.PaidAmount),
                    RemainingAmount = g.Sum(x => x.item.RemainingAmount)
                };
            })
            .OrderByDescending(x => x.RemainingAmount)
            .ToList();

        return result;
    }
}

