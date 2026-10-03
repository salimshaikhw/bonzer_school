using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.ViewModels.Attendance;

namespace Application.Abstractions.Services;

public interface IAttendanceHolidayService
{
    Task<AttendanceWeeklyOffSettingsDto> GetWeeklyOffSettingsAsync(Guid branchId);
    Task UpdateWeeklyOffSettingsAsync(Guid branchId, UpdateAttendanceWeeklyOffSettingsRequest request);
    Task<IReadOnlyList<SchoolHolidayResponse>> ListHolidaysAsync(Guid branchId, DateOnly? from, DateOnly? to);
    Task<SchoolHolidayResponse> AddHolidayAsync(Guid branchId, SchoolHolidayRequest request);
    Task<bool> DeleteHolidayAsync(Guid id);
    Task<int> CommitAsync();
}
