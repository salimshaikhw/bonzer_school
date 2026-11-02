using Application.Abstractions.Repositories;
using Application.Abstractions.Services.Attendances;
using Application.Abstractions.Services;
using Application.Abstractions;
using Application.Attendances;
using Application.Common.Extensions;
using Application.Common.Helpers.MemoryCache;
using Application.Common.Helpers;
using Application.ViewModels.Attendance;
using AutoMapper;
using Domain.Enums;
using Infrastructure.Persistence.Postgres;
using Infrastructure;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace Infrastructure.Services.Attendances
{
    /// <summary>Dependencies shared by the attendance services (Parameter Object, registered once in DI).</summary>
    public sealed record AttendanceServiceDependencies(
        IAttendanceRepository Attendances,
        IStudentRepository Students,
        IAcademicYearRepository AcademicYears,
        IStudentResultRepository StudentResults,
        ILeaveRequestRepository LeaveRequests,
        IAttendanceCorrectionRequestRepository CorrectionRequests,
        IAttendanceDateLockRepository DateLocks,
        IAttendanceAcademicYearLockRepository AcademicYearLocks,
        IBranchRepository Branches,
        IClassRepository Classes,
        IDivisionRepository Divisions,
        ISchoolHolidayRepository SchoolHolidays,
        IClassTeacherMapRepository ClassTeacherMaps,
        IStaffRepository Staff,
        IMapper Mapper,
        IMemoryCacheHelper Cache,
        IUnitOfWork UnitOfWork,
        IAttendancePolicy Policy);

    /// <summary>
    /// Base of the attendance services, split by responsibility (SRP / ISP):
    ///   AttendanceService        - marking, imports, locks, edits, leave / correction workflows (IAttendanceService)
    ///   AttendanceReportService  - reports, summaries, counts, trends, dashboards and exports (IAttendanceReportService)
    /// The weekly-off / holiday calendar and record helpers live here so both sides count days the same way.
    /// </summary>
    public abstract class AttendanceServiceBase
    {
        protected readonly IAttendanceRepository _attendanceRepository;
        protected readonly IStudentRepository _studentRepository;
        protected readonly IAcademicYearRepository _academicYearRepository;
        protected readonly IStudentResultRepository _studentResultRepository;
        protected readonly ILeaveRequestRepository _leaveRequestRepository;
        protected readonly IAttendanceCorrectionRequestRepository _correctionRequestRepository;
        protected readonly IAttendanceDateLockRepository _dateLockRepository;
        protected readonly IAttendanceAcademicYearLockRepository _academicYearLockRepository;
        protected readonly IMapper _mapper;
        protected readonly IMemoryCacheHelper _cache;
        protected readonly IUnitOfWork _unitOfWork;
        protected readonly IBranchRepository _branchRepository;
        protected readonly IClassRepository _classRepository;
        protected readonly IDivisionRepository _divisionRepository;
        protected readonly ISchoolHolidayRepository _schoolHolidayRepository;
        protected readonly IClassTeacherMapRepository _classTeacherMapRepository;
        protected readonly IStaffRepository _staffRepository;
        protected readonly IAttendancePolicy _policy;
        protected readonly ILogger _logger;

        protected AttendanceServiceBase(AttendanceServiceDependencies deps, ILogger logger)
        {
            _attendanceRepository = deps.Attendances;
            _studentRepository = deps.Students;
            _academicYearRepository = deps.AcademicYears;
            _studentResultRepository = deps.StudentResults;
            _leaveRequestRepository = deps.LeaveRequests;
            _correctionRequestRepository = deps.CorrectionRequests;
            _dateLockRepository = deps.DateLocks;
            _academicYearLockRepository = deps.AcademicYearLocks;
            _mapper = deps.Mapper;
            _cache = deps.Cache;
            _unitOfWork = deps.UnitOfWork;
            _branchRepository = deps.Branches;
            _classRepository = deps.Classes;
            _divisionRepository = deps.Divisions;
            _schoolHolidayRepository = deps.SchoolHolidays;
            _classTeacherMapRepository = deps.ClassTeacherMaps;
            _staffRepository = deps.Staff;
            _policy = deps.Policy;
            _logger = logger;
        }


        protected readonly record struct AttendanceCalendarContext(AttendanceSaturdayPattern SaturdayPattern, bool SundayOff, HashSet<DateOnly> CalendarHolidayDates);

        protected async Task<(DateOnly start, DateOnly end)> ResolveAcademicYearRangeAsync(Guid? studentId)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (studentId.HasValue)
            {
                var student = await _studentRepository.GetByIdAsync(studentId.Value);
                if (student != null && student.AcademicYearId.HasValue)
                {
                    var ay = await _academicYearRepository.GetByIdAsync(student.AcademicYearId.Value);
                    if (ay?.StartDate.HasValue == true && ay.EndDate.HasValue)
                        return (ay.StartDate.Value, ay.EndDate.Value);
                }
            }
            var allYears = await _academicYearRepository.FindAllAsync(ay => ay.IsActive == true);
            var current = allYears?.FirstOrDefault(ay => ay.StartDate <= today && ay.EndDate >= today);
            if (current?.StartDate.HasValue == true && current.EndDate.HasValue)
                return (current.StartDate.Value, current.EndDate.Value);
            var active = allYears?.FirstOrDefault();
            if (active?.StartDate.HasValue == true && active.EndDate.HasValue)
                return (active.StartDate.Value, active.EndDate.Value);
            return (today.AddMonths(-6), today.AddMonths(6));
        }

        protected async Task<List<Attendance>> GetAttendanceRecordsInRangeAsync(Guid studentId, DateOnly start, DateOnly end)
        {
            var list = await _attendanceRepository.FindAllAsync(a =>
                a.StudentId == studentId &&
                a.AttendanceDate >= start &&
                a.AttendanceDate <= end &&
                a.IsDeleted != true);
            return list ?? new List<Attendance>();
        }

        protected async Task<List<Attendance>> GetClassAttendanceRecordsInRangeAsync(Guid classId, DateOnly start, DateOnly end)
        {
            var list = await _attendanceRepository.FindAllAsync(a =>
                a.ClassId == classId &&
                a.AttendanceDate >= start &&
                a.AttendanceDate <= end &&
                a.IsDeleted != true);
            return list ?? new List<Attendance>();
        }

        protected async Task<List<Attendance>> GetDivisionAttendanceRecordsInRangeAsync(Guid divisionId, DateOnly start, DateOnly end)
        {
            var list = await _attendanceRepository.FindAllAsync(a =>
                a.DivisionId == divisionId &&
                a.AttendanceDate >= start &&
                a.AttendanceDate <= end &&
                a.IsDeleted != true);
            return list ?? new List<Attendance>();
        }

        protected async Task<List<Attendance>> GetBranchAttendanceRecordsInRangeAsync(Guid branchId, DateOnly start, DateOnly end)
        {
            var students = await _studentRepository.FindAllAsync(s => s.BranchId == branchId);
            if (students == null || students.Count == 0) return new List<Attendance>();
            var studentIds = students.Select(s => s.Id).ToHashSet();
            var all = await _attendanceRepository.FindAllAsync(a =>
                a.StudentId.HasValue && studentIds.Contains(a.StudentId.Value) &&
                a.AttendanceDate >= start && a.AttendanceDate <= end &&
                a.IsDeleted != true);
            return all ?? new List<Attendance>();
        }

        protected static bool IsStatus(string? status, string value)
        {
            return string.Equals(status, value, StringComparison.OrdinalIgnoreCase);
        }

        protected async Task<(AttendanceSaturdayPattern SaturdayPattern, bool SundayOff)> ResolveWeeklyOffAsync(Guid? branchId)
        {
            if (!branchId.HasValue || branchId.Value == Guid.Empty)
                return (AttendanceSaturdayPattern.None, true);
            var b = await _branchRepository.GetByIdAsync(branchId.Value);
            if (b == null) return (AttendanceSaturdayPattern.None, true);
            var sun = b.AttendanceSundayOff != false;
            return (EffectiveSaturdayPattern(b), sun);
        }

        protected static AttendanceSaturdayPattern EffectiveSaturdayPattern(Branch b)
        {
            if (b.AttendanceSaturdayPattern != AttendanceSaturdayPattern.None)
                return b.AttendanceSaturdayPattern;
            return b.AttendanceSaturdayOff == true ? AttendanceSaturdayPattern.AllSaturdays : AttendanceSaturdayPattern.None;
        }

        protected async Task<AttendanceCalendarContext> ResolveCalendarAsync(Guid? branchId, DateOnly start, DateOnly end)
        {
            var w = await ResolveWeeklyOffAsync(branchId);
            HashSet<DateOnly> holidays = new();
            if (branchId.HasValue && branchId.Value != Guid.Empty)
                holidays = await _schoolHolidayRepository.GetActiveDatesInRangeAsync(branchId.Value, start, end);
            return new AttendanceCalendarContext(w.SaturdayPattern, w.SundayOff, holidays);
        }

        protected async Task<Guid?> ResolveBranchIdForStudentAsync(Guid studentId)
        {
            var s = await _studentRepository.GetByIdAsync(studentId);
            return s?.BranchId;
        }

        protected async Task<Guid?> ResolveBranchIdForClassAsync(Guid classId)
        {
            var c = await _classRepository.GetByIdAsync(classId);
            return c?.BranchId;
        }

        protected async Task<Guid?> ResolveBranchIdForDivisionAsync(Guid divisionId)
        {
            var d = await _divisionRepository.GetByIdAsync(divisionId);
            return d?.BranchId;
        }

        /// <summary>1-based index of this Saturday within its calendar month (first Saturday = 1).</summary>
        protected static int SaturdayOrdinalInMonth(DateOnly d)
        {
            if (d.DayOfWeek != DayOfWeek.Saturday) return 0;
            var first = new DateOnly(d.Year, d.Month, 1);
            var dow1 = (int)first.DayOfWeek;
            var dowSat = (int)DayOfWeek.Saturday;
            var daysToFirstSat = (dowSat - dow1 + 7) % 7;
            var firstSat = first.AddDays(daysToFirstSat);
            return (d.DayNumber - firstSat.DayNumber) / 7 + 1;
        }

        protected static bool IsSaturdayWeeklyOff(DateOnly d, AttendanceSaturdayPattern p)
        {
            if (d.DayOfWeek != DayOfWeek.Saturday) return false;
            var n = SaturdayOrdinalInMonth(d);
            return p switch
            {
                AttendanceSaturdayPattern.None => false,
                AttendanceSaturdayPattern.AllSaturdays => true,
                AttendanceSaturdayPattern.SecondFourth => n is 2 or 4,
                AttendanceSaturdayPattern.FirstThirdFifth => n is 1 or 3 or 5,
                _ => false
            };
        }

        protected static bool IsWeeklyOffDay(DateOnly d, AttendanceSaturdayPattern satPattern, bool sundayOff) =>
            IsSaturdayWeeklyOff(d, satPattern) || (sundayOff && d.DayOfWeek == DayOfWeek.Sunday);

        protected static string? WeeklyOffTrendLabel(DateOnly d, AttendanceSaturdayPattern satPattern, bool sunOff)
        {
            if (IsSaturdayWeeklyOff(d, satPattern)) return "Saturday";
            if (sunOff && d.DayOfWeek == DayOfWeek.Sunday) return "Sunday";
            return null;
        }

        protected static string? NonWorkingCalendarCell(DateOnly d, AttendanceCalendarContext ctx)
        {
            if (ctx.CalendarHolidayDates.Contains(d)) return "Holiday";
            return WeeklyOffTrendLabel(d, ctx.SaturdayPattern, ctx.SundayOff);
        }

        /// <summary>
        /// Working-day rows for attendance stats: excludes branch weekly off, calendar holidays, row <see cref="Attendance.IsHoliday"/>, and status Holiday.
        /// </summary>
        protected static bool CountsTowardWorkingDayStats(Attendance r, AttendanceCalendarContext ctx)
        {
            if (r.AttendanceDate is not { } d) return false;
            if (r.IsHoliday == true) return false;
            if (IsStatus(r.Status, "Holiday")) return false;
            if (ctx.CalendarHolidayDates.Contains(d)) return false;
            if (IsWeeklyOffDay(d, ctx.SaturdayPattern, ctx.SundayOff)) return false;
            return true;
        }

        protected static string FormatClassDayTrendLabel(DateOnly d, IReadOnlyList<Attendance>? dayRecords, AttendanceCalendarContext ctx)
        {
            if (ctx.CalendarHolidayDates.Contains(d)) return "Holiday";
            var w = WeeklyOffTrendLabel(d, ctx.SaturdayPattern, ctx.SundayOff);
            if (w != null) return w;
            if (dayRecords == null || dayRecords.Count == 0) return "—";
            var working = dayRecords.Where(r => CountsTowardWorkingDayStats(r, ctx)).ToList();
            if (working.Count == 0)
            {
                if (dayRecords.Any(r => r.IsHoliday == true || IsStatus(r.Status, "Holiday"))) return "Holiday";
                return "—";
            }
            var pct = AttendanceTally.Of(working.Select(r => r.Status)).Percentage;
            return pct.ToString("F0") + "%";
        }

        protected static AttendanceSummaryDto BuildSummaryFromRecords(List<Attendance> records, AttendanceCalendarContext ctx)
        {
            var tally = AttendanceTally.Of(records.Where(r => CountsTowardWorkingDayStats(r, ctx)).Select(r => r.Status));
            return new AttendanceSummaryDto
            {
                PresentCount = tally.Present, AbsentCount = tally.Absent, LeaveCount = tally.Leave, HalfDayCount = tally.HalfDay,
                LateCount = tally.Late, TotalDays = tally.WorkingDays, Percentage = tally.Percentage,
            };
        }

        protected async Task<int> GetPendingLeaveApprovalsForBranchAsync(Guid branchId)
        {
            var students = await _studentRepository.FindAllAsync(s => s.BranchId == branchId);
            if (students == null || students.Count == 0) return 0;
            var ids = students.Select(s => s.Id).ToHashSet();
            var all = await _leaveRequestRepository.FindAllAsync(l => l.Status == "Pending");
            return all?.Count(l => ids.Contains(l.StudentId)) ?? 0;
        }

        protected async Task<int> GetPendingCorrectionCountForBranchAsync(Guid branchId)
        {
            var all = await _correctionRequestRepository.FindAllAsync(c => c.Status == "Pending");
            return all?.Count ?? 0;
        }

        protected static LeaveRequestDto MapToLeaveRequestDto(LeaveRequest l, Student? s)
        {
            return new LeaveRequestDto
            {
                StudentId = l.StudentId, StudentName = s != null ? $"{s.FirstName} {s.LastName}".Trim() : null,
                FromDate = l.FromDate, ToDate = l.ToDate, LeaveType = l.LeaveType, Reason = l.Reason, Status = l.Status,
                RequestedAt = l.RequestedAt, RequestedBy = l.RequestedBy, ApprovedAt = l.ApprovedAt, ApprovedBy = l.ApprovedBy, RejectionReason = l.RejectionReason
            }.WithAuditFrom(l);
        }

        protected static ExportResultDto BuildCsvExport<T>(string fileName, IEnumerable<T> rows)
        {
            var sb = new System.Text.StringBuilder();
            var first = true;
            foreach (var r in rows)
            {
                if (first) { sb.AppendLine(string.Join(",", typeof(T).GetProperties().Select(p => p.Name))); first = false; }
                sb.AppendLine(string.Join(",", typeof(T).GetProperties().Select(p => p.GetValue(r)?.ToString() ?? "")));
            }
            var content = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            return new ExportResultDto { Content = content, FileName = fileName, ContentType = "text/csv" };
        }
    }
}
