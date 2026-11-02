using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Abstractions.Repositories.Attendances;

public interface ISchoolHolidayRepository : IRepository<SchoolHoliday>
{
    Task<HashSet<DateOnly>> GetActiveDatesInRangeAsync(Guid branchId, DateOnly start, DateOnly end);

    Task<bool> HasActiveHolidayAsync(Guid branchId, DateOnly date);

    Task<List<SchoolHoliday>> ListActiveInRangeAsync(Guid branchId, DateOnly? from, DateOnly? to);
}
