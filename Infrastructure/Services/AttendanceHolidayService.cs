using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Abstractions;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Helpers.MemoryCache;
using Application.ViewModels.Attendance;
using Domain.Entities.Postgres.Scaffolded;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class AttendanceHolidayService : IAttendanceHolidayService
{
    private readonly IBranchRepository _branchRepository;
    private readonly ISchoolHolidayRepository _schoolHolidayRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemoryCacheHelper _cache;
    private readonly ILogger<AttendanceHolidayService> _logger;

    public AttendanceHolidayService(
        IBranchRepository branchRepository,
        ISchoolHolidayRepository schoolHolidayRepository,
        IUnitOfWork unitOfWork,
        IMemoryCacheHelper cache,
        ILogger<AttendanceHolidayService> logger)
    {
        _branchRepository = branchRepository;
        _schoolHolidayRepository = schoolHolidayRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _logger = logger;
    }

    public async Task<AttendanceWeeklyOffSettingsDto> GetWeeklyOffSettingsAsync(Guid branchId)
    {
        var b = await _branchRepository.GetByIdAsync(branchId)
            ?? throw new Application.Exceptions.BaseServiceException("Branch not found", Application.Common.ExceptionCodes.ItemNotFound);
        return new AttendanceWeeklyOffSettingsDto
        {
            BranchId = branchId,
            SaturdayPattern = ToApiSaturdayPattern(EffectiveSaturdayPattern(b)),
            SundayOff = b.AttendanceSundayOff != false
        };
    }

    public async Task UpdateWeeklyOffSettingsAsync(Guid branchId, UpdateAttendanceWeeklyOffSettingsRequest request)
    {
        var b = await _branchRepository.GetByIdAsync(branchId)
            ?? throw new Application.Exceptions.BaseServiceException("Branch not found", Application.Common.ExceptionCodes.ItemNotFound);
        var pattern = ParseSaturdayPattern(request.SaturdayPattern);
        b.AttendanceSaturdayPattern = pattern;
        b.AttendanceSaturdayOff = pattern == AttendanceSaturdayPattern.AllSaturdays;
        b.AttendanceSundayOff = request.SundayOff;
        await _branchRepository.UpdateAsync(b);
        ClearBranchAndAttendanceCaches(branchId);
        _logger.LogInformation("Updated weekly off for branch {BranchId}: SatPattern={Sat}, Sun={Sun}", branchId, pattern, request.SundayOff);
    }

    public async Task<IReadOnlyList<SchoolHolidayResponse>> ListHolidaysAsync(Guid branchId, DateOnly? from, DateOnly? to)
    {
        await EnsureBranchExists(branchId);
        var list = await _schoolHolidayRepository.ListActiveInRangeAsync(branchId, from, to);
        return list.Select(Map).ToList();
    }

    public async Task<SchoolHolidayResponse> AddHolidayAsync(Guid branchId, SchoolHolidayRequest request)
    {
        await EnsureBranchExists(branchId);
        var name = string.IsNullOrWhiteSpace(request.Name) ? "Holiday" : request.Name.Trim();
        var existing = await _schoolHolidayRepository.FindAsync(
            h => h.BranchId == branchId && h.HolidayDate == request.HolidayDate,
            trackChanges: true);
        if (existing != null)
        {
            if (existing.IsDeleted == true)
            {
                existing.IsDeleted = false;
                existing.Name = name;
                await _schoolHolidayRepository.UpdateAsync(existing);
                ClearBranchAndAttendanceCaches(branchId);
                return Map(existing);
            }
            throw new Application.Exceptions.BaseServiceException("A holiday already exists on this date for this branch.", Application.Common.ExceptionCodes.Validation);
        }
        var entity = new SchoolHoliday
        {
            Id = Guid.NewGuid(),
            BranchId = branchId,
            HolidayDate = request.HolidayDate,
            Name = name,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
        await _schoolHolidayRepository.AddAsync(entity);
        ClearBranchAndAttendanceCaches(branchId);
        return Map(entity);
    }

    public async Task<bool> DeleteHolidayAsync(Guid id)
    {
        var h = await _schoolHolidayRepository.FindAsync(x => x.Id == id, trackChanges: true);
        if (h == null || h.IsDeleted == true) return false;
        h.IsDeleted = true;
        await _schoolHolidayRepository.UpdateAsync(h);
        ClearBranchAndAttendanceCaches(h.BranchId);
        return true;
    }

    public Task<int> CommitAsync() => _unitOfWork.Commit();

    private async Task EnsureBranchExists(Guid branchId)
    {
        if (await _branchRepository.GetByIdAsync(branchId) == null)
            throw new Application.Exceptions.BaseServiceException("Branch not found", Application.Common.ExceptionCodes.ItemNotFound);
    }

    private void ClearBranchAndAttendanceCaches(Guid branchId)
    {
        _cache.RemoveCache("branches_all");
        _cache.RemoveCache($"branch_{branchId}");
        _cache.RemoveCache("attendances_all");
        _cache.RemoveCache($"attendance_dashboard_{branchId}");
    }

    private static SchoolHolidayResponse Map(SchoolHoliday h) => new()
    {
        Id = h.Id,
        BranchId = h.BranchId,
        HolidayDate = h.HolidayDate,
        Name = h.Name
    };

    private static AttendanceSaturdayPattern EffectiveSaturdayPattern(Branch b)
    {
        if (b.AttendanceSaturdayPattern != AttendanceSaturdayPattern.None)
            return b.AttendanceSaturdayPattern;
        return b.AttendanceSaturdayOff == true ? AttendanceSaturdayPattern.AllSaturdays : AttendanceSaturdayPattern.None;
    }

    private static AttendanceSaturdayPattern ParseSaturdayPattern(string? raw)
    {
        return raw?.Trim().ToLowerInvariant() switch
        {
            "allsaturdays" or "all" => AttendanceSaturdayPattern.AllSaturdays,
            "secondfourth" or "second_fourth" => AttendanceSaturdayPattern.SecondFourth,
            "firstthirdfifth" or "first_third_fifth" => AttendanceSaturdayPattern.FirstThirdFifth,
            _ => AttendanceSaturdayPattern.None
        };
    }

    private static string ToApiSaturdayPattern(AttendanceSaturdayPattern p) => p switch
    {
        AttendanceSaturdayPattern.AllSaturdays => "allSaturdays",
        AttendanceSaturdayPattern.SecondFourth => "secondFourth",
        AttendanceSaturdayPattern.FirstThirdFifth => "firstThirdFifth",
        _ => "none"
    };
}
