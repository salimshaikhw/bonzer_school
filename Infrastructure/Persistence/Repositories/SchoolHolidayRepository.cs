using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Abstractions.Repositories;
using Domain.Entities.Postgres.Scaffolded;
using Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories;

public class SchoolHolidayRepository : Repository<SchoolHoliday>, ISchoolHolidayRepository
{
    public SchoolHolidayRepository(BonzerSchoolDbContext context, ILogger<SchoolHolidayRepository> logger)
        : base(context, logger)
    {
    }

    public async Task<HashSet<DateOnly>> GetActiveDatesInRangeAsync(Guid branchId, DateOnly start, DateOnly end)
    {
        var dates = await _dbSet.AsNoTracking()
            .Where(h => h.BranchId == branchId && h.IsDeleted != true && h.HolidayDate >= start && h.HolidayDate <= end)
            .Select(h => h.HolidayDate)
            .ToListAsync();
        return dates.ToHashSet();
    }

    public async Task<bool> HasActiveHolidayAsync(Guid branchId, DateOnly date)
    {
        return await _dbSet.AsNoTracking()
            .AnyAsync(h => h.BranchId == branchId && h.IsDeleted != true && h.HolidayDate == date);
    }

    public async Task<List<SchoolHoliday>> ListActiveInRangeAsync(Guid branchId, DateOnly? from, DateOnly? to)
    {
        var q = _dbSet.AsNoTracking().Where(h => h.BranchId == branchId && h.IsDeleted != true);
        if (from.HasValue) q = q.Where(h => h.HolidayDate >= from.Value);
        if (to.HasValue) q = q.Where(h => h.HolidayDate <= to.Value);
        return await q.OrderBy(h => h.HolidayDate).ToListAsync();
    }
}
