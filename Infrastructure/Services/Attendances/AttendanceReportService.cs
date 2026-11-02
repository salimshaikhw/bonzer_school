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
    /// <summary>Attendance reports, summaries, counts, trends, dashboards and exports (read only).</summary>
    public class AttendanceReportService : AttendanceServiceBase, IAttendanceReportService
    {
        public AttendanceReportService(AttendanceServiceDependencies deps, ILogger<AttendanceReportService> logger)
            : base(deps, logger)
        {
        }


        public async Task<AttendanceDateRangeDto> ResolveDateRangeAsync(AttendancePeriodType periodType, DateTime? startDate, DateTime? endDate, Guid? studentId = null)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            DateOnly start;
            DateOnly end;

            switch (periodType)
            {
                case AttendancePeriodType.Day:
                    if (startDate.HasValue)
                    {
                        start = end = DateOnly.FromDateTime(startDate.Value);
                    }
                    else
                    {
                        start = end = today;
                    }
                    break;
                case AttendancePeriodType.Week:
                    if (startDate.HasValue)
                    {
                        start = DateOnly.FromDateTime(startDate.Value);
                        end = start.AddDays(6);
                    }
                    else
                    {
                        end = today;
                        start = today.AddDays(-6);
                    }
                    break;
                case AttendancePeriodType.Month:
                    if (startDate.HasValue)
                    {
                        var d = DateOnly.FromDateTime(startDate.Value);
                        start = new DateOnly(d.Year, d.Month, 1);
                        end = start.AddMonths(1).AddDays(-1);
                    }
                    else
                    {
                        start = new DateOnly(today.Year, today.Month, 1);
                        end = start.AddMonths(1).AddDays(-1);
                    }
                    break;
                case AttendancePeriodType.AcademicYear:
                    var (ayStart, ayEnd) = await ResolveAcademicYearRangeAsync(studentId);
                    start = ayStart;
                    end = ayEnd;
                    break;
                case AttendancePeriodType.Custom:
                default:
                    if (startDate.HasValue && endDate.HasValue)
                    {
                        start = DateOnly.FromDateTime(startDate.Value);
                        end = DateOnly.FromDateTime(endDate.Value);
                        if (start > end) (start, end) = (end, start);
                    }
                    else
                    {
                        start = end = today;
                    }
                    break;
            }

            return new AttendanceDateRangeDto { StartDate = start, EndDate = end };
        }

        public async Task<AttendanceReportDto> GetStudentAttendanceReportAsync(Guid studentId, AttendancePeriodType periodType, DateTime? startDate, DateTime? endDate)
        {
            var range = await ResolveDateRangeAsync(periodType, startDate, endDate, studentId);
            var summary = await GetStudentAttendanceSummaryAsync(studentId, periodType, startDate, endDate);
            var list = await GetStudentAttendanceDetailedListAsync(studentId, periodType, startDate, endDate);
            return new AttendanceReportDto
            {
                PeriodType = periodType,
                StartDate = range.StartDate,
                EndDate = range.EndDate,
                Summary = summary,
                DetailedList = list
            };
        }

        public async Task<AttendanceSummaryDto> GetStudentAttendanceSummaryAsync(Guid studentId, AttendancePeriodType periodType, DateTime? startDate, DateTime? endDate)
        {
            var range = await ResolveDateRangeAsync(periodType, startDate, endDate, studentId);
            var records = await GetAttendanceRecordsInRangeAsync(studentId, range.StartDate, range.EndDate);
            var branchId = await ResolveBranchIdForStudentAsync(studentId);
            var ctx = await ResolveCalendarAsync(branchId, range.StartDate, range.EndDate);
            return BuildSummaryFromRecords(records, ctx);
        }

        public async Task<IEnumerable<AttendanceResponse>> GetStudentAttendanceDetailedListAsync(Guid studentId, AttendancePeriodType periodType, DateTime? startDate, DateTime? endDate)
        {
            var range = await ResolveDateRangeAsync(periodType, startDate, endDate, studentId);
            var records = await GetAttendanceRecordsInRangeAsync(studentId, range.StartDate, range.EndDate);
            return records.Select(_mapper.Map<AttendanceResponse>).OrderBy(r => r.AttendanceDate).ToList();
        }

        public async Task<decimal> GetStudentAttendancePercentageAsync(Guid studentId, AttendancePeriodType periodType)
        {
            var summary = await GetStudentAttendanceSummaryAsync(studentId, periodType, null, null);
            return summary.Percentage;
        }

        public async Task<int> GetStudentPresentCountAsync(Guid studentId, AttendancePeriodType periodType)
        {
            var range = await ResolveDateRangeAsync(periodType, null, null, studentId);
            var records = await GetAttendanceRecordsInRangeAsync(studentId, range.StartDate, range.EndDate);
            var branchId = await ResolveBranchIdForStudentAsync(studentId);
            var ctx = await ResolveCalendarAsync(branchId, range.StartDate, range.EndDate);
            return records.Count(r => CountsTowardWorkingDayStats(r, ctx) && IsStatus(r.Status, "Present"));
        }

        public async Task<int> GetStudentAbsentCountAsync(Guid studentId, AttendancePeriodType periodType)
        {
            var range = await ResolveDateRangeAsync(periodType, null, null, studentId);
            var records = await GetAttendanceRecordsInRangeAsync(studentId, range.StartDate, range.EndDate);
            var branchId = await ResolveBranchIdForStudentAsync(studentId);
            var ctx = await ResolveCalendarAsync(branchId, range.StartDate, range.EndDate);
            return records.Count(r => CountsTowardWorkingDayStats(r, ctx) && IsStatus(r.Status, "Absent"));
        }

        public async Task<int> GetStudentLeaveCountAsync(Guid studentId, AttendancePeriodType periodType)
        {
            var range = await ResolveDateRangeAsync(periodType, null, null, studentId);
            var records = await GetAttendanceRecordsInRangeAsync(studentId, range.StartDate, range.EndDate);
            var branchId = await ResolveBranchIdForStudentAsync(studentId);
            var ctx = await ResolveCalendarAsync(branchId, range.StartDate, range.EndDate);
            return records.Count(r => CountsTowardWorkingDayStats(r, ctx) && IsStatus(r.Status, "Leave"));
        }

        public async Task<int> GetStudentHalfDayCountAsync(Guid studentId, AttendancePeriodType periodType)
        {
            var range = await ResolveDateRangeAsync(periodType, null, null, studentId);
            var records = await GetAttendanceRecordsInRangeAsync(studentId, range.StartDate, range.EndDate);
            var branchId = await ResolveBranchIdForStudentAsync(studentId);
            var ctx = await ResolveCalendarAsync(branchId, range.StartDate, range.EndDate);
            return records.Count(r => CountsTowardWorkingDayStats(r, ctx) && IsStatus(r.Status, "HalfDay"));
        }

        public async Task<int> GetStudentConsecutiveAbsenceDaysAsync(Guid studentId)
        {
            var records = await _attendanceRepository.FindAllAsync(a =>
                a.StudentId == studentId && a.IsDeleted != true && a.AttendanceDate.HasValue);
            var ordered = records.OrderByDescending(a => a.AttendanceDate).ToList();
            if (ordered.Count == 0) return 0;
            var branchId = await ResolveBranchIdForStudentAsync(studentId);
            var min = ordered.Min(a => a.AttendanceDate!.Value);
            var max = ordered.Max(a => a.AttendanceDate!.Value);
            var ctx = await ResolveCalendarAsync(branchId, min, max);
            var maxStreak = 0;
            var current = 0;
            foreach (var r in ordered)
            {
                if (!CountsTowardWorkingDayStats(r, ctx)) continue;
                if (IsStatus(r.Status, "Absent"))
                    current++;
                else
                {
                    if (current > maxStreak) maxStreak = current;
                    current = 0;
                }
            }
            if (current > maxStreak) maxStreak = current;
            return maxStreak;
        }

        public async Task<int> GetStudentLateCountAsync(Guid studentId)
        {
            var records = await _attendanceRepository.FindAllAsync(a =>
                a.StudentId == studentId && a.IsDeleted != true && a.AttendanceDate.HasValue);
            if (records == null || records.Count == 0) return 0;
            var branchId = await ResolveBranchIdForStudentAsync(studentId);
            var min = records.Min(a => a.AttendanceDate!.Value);
            var max = records.Max(a => a.AttendanceDate!.Value);
            var ctx = await ResolveCalendarAsync(branchId, min, max);
            return records.Count(r => CountsTowardWorkingDayStats(r, ctx) && IsStatus(r.Status, "Late"));
        }

        public async Task<bool> IsStudentLowAttendanceAsync(Guid studentId, decimal threshold)
        {
            var pct = await GetStudentAttendancePercentageAsync(studentId, AttendancePeriodType.AcademicYear);
            return pct < threshold;
        }

        public async Task<bool> IsStudentChronicAbsenteeAsync(Guid studentId, int consecutiveDays)
        {
            var count = await GetStudentConsecutiveAbsenceDaysAsync(studentId);
            return count >= consecutiveDays;
        }

        public async Task<IEnumerable<AttendanceTrendPointDto>> GetStudentAttendanceTrendAsync(Guid studentId, AttendancePeriodType periodType)
        {
            var range = await ResolveDateRangeAsync(periodType, null, null, studentId);
            var records = await GetAttendanceRecordsInRangeAsync(studentId, range.StartDate, range.EndDate);
            var branchId = await ResolveBranchIdForStudentAsync(studentId);
            var ctx = await ResolveCalendarAsync(branchId, range.StartDate, range.EndDate);
            var byDate = records
                .Where(a => a.AttendanceDate.HasValue)
                .GroupBy(a => a.AttendanceDate!.Value)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedAt).First());
            var list = new List<AttendanceTrendPointDto>();
            for (var d = range.StartDate; d <= range.EndDate; d = d.AddDays(1))
            {
                string label;
                if (ctx.CalendarHolidayDates.Contains(d)) label = "Holiday";
                else if (WeeklyOffTrendLabel(d, ctx.SaturdayPattern, ctx.SundayOff) is { } wo) label = wo;
                else if (!byDate.TryGetValue(d, out var row)) label = "—";
                else if (row.IsHoliday == true || IsStatus(row.Status, "Holiday")) label = "Holiday";
                else label = row.Status ?? "—";
                list.Add(new AttendanceTrendPointDto { Date = d, Status = label });
            }
            return list.OrderBy(x => x.Date).ToList();
        }

        public async Task<AttendanceReportDto> GetClassAttendanceReportAsync(Guid classId, AttendancePeriodType periodType, DateTime? startDate, DateTime? endDate)
        {
            var range = await ResolveDateRangeAsync(periodType, startDate, endDate, null);
            var summary = await GetClassAttendanceSummaryAsync(classId, periodType, startDate, endDate);
            var list = await GetClassAttendanceDetailedListAsync(classId, periodType, startDate, endDate);
            return new AttendanceReportDto
            {
                PeriodType = periodType,
                StartDate = range.StartDate,
                EndDate = range.EndDate,
                Summary = summary,
                DetailedList = list
            };
        }

        public async Task<AttendanceSummaryDto> GetClassAttendanceSummaryAsync(Guid classId, AttendancePeriodType periodType, DateTime? startDate, DateTime? endDate)
        {
            var range = await ResolveDateRangeAsync(periodType, startDate, endDate, null);
            var records = await GetClassAttendanceRecordsInRangeAsync(classId, range.StartDate, range.EndDate);
            var branchId = await ResolveBranchIdForClassAsync(classId);
            var ctx = await ResolveCalendarAsync(branchId, range.StartDate, range.EndDate);
            return BuildSummaryFromRecords(records, ctx);
        }

        public async Task<IEnumerable<AttendanceResponse>> GetClassAttendanceDetailedListAsync(Guid classId, AttendancePeriodType periodType, DateTime? startDate, DateTime? endDate)
        {
            var range = await ResolveDateRangeAsync(periodType, startDate, endDate, null);
            var records = await GetClassAttendanceRecordsInRangeAsync(classId, range.StartDate, range.EndDate);
            return records.Select(_mapper.Map<AttendanceResponse>).OrderBy(r => r.AttendanceDate).ThenBy(r => r.StudentId).ToList();
        }

        public async Task<decimal> GetClassAttendancePercentageAsync(Guid classId, AttendancePeriodType periodType)
        {
            var summary = await GetClassAttendanceSummaryAsync(classId, periodType, null, null);
            return summary.Percentage;
        }

        public async Task<int> GetClassPresentCountAsync(Guid classId, AttendancePeriodType periodType)
        {
            var range = await ResolveDateRangeAsync(periodType, null, null, null);
            var records = await GetClassAttendanceRecordsInRangeAsync(classId, range.StartDate, range.EndDate);
            var branchId = await ResolveBranchIdForClassAsync(classId);
            var ctx = await ResolveCalendarAsync(branchId, range.StartDate, range.EndDate);
            return records.Count(r => CountsTowardWorkingDayStats(r, ctx) && IsStatus(r.Status, "Present"));
        }

        public async Task<int> GetClassAbsentCountAsync(Guid classId, AttendancePeriodType periodType)
        {
            var range = await ResolveDateRangeAsync(periodType, null, null, null);
            var records = await GetClassAttendanceRecordsInRangeAsync(classId, range.StartDate, range.EndDate);
            var branchId = await ResolveBranchIdForClassAsync(classId);
            var ctx = await ResolveCalendarAsync(branchId, range.StartDate, range.EndDate);
            return records.Count(r => CountsTowardWorkingDayStats(r, ctx) && IsStatus(r.Status, "Absent"));
        }

        public async Task<int> GetClassLeaveCountAsync(Guid classId, AttendancePeriodType periodType)
        {
            var range = await ResolveDateRangeAsync(periodType, null, null, null);
            var records = await GetClassAttendanceRecordsInRangeAsync(classId, range.StartDate, range.EndDate);
            var branchId = await ResolveBranchIdForClassAsync(classId);
            var ctx = await ResolveCalendarAsync(branchId, range.StartDate, range.EndDate);
            return records.Count(r => CountsTowardWorkingDayStats(r, ctx) && IsStatus(r.Status, "Leave"));
        }

        public async Task<int> GetClassHalfDayCountAsync(Guid classId, AttendancePeriodType periodType)
        {
            var range = await ResolveDateRangeAsync(periodType, null, null, null);
            var records = await GetClassAttendanceRecordsInRangeAsync(classId, range.StartDate, range.EndDate);
            var branchId = await ResolveBranchIdForClassAsync(classId);
            var ctx = await ResolveCalendarAsync(branchId, range.StartDate, range.EndDate);
            return records.Count(r => CountsTowardWorkingDayStats(r, ctx) && IsStatus(r.Status, "HalfDay"));
        }

        public async Task<IEnumerable<ClassStudentAttendanceSummaryDto>> GetClassLowAttendanceStudentsAsync(Guid classId, decimal threshold, AttendancePeriodType periodType)
        {
            var students = await _studentRepository.FindAllAsync(s => s.ClassId == classId);
            if (students == null || students.Count == 0) return Array.Empty<ClassStudentAttendanceSummaryDto>();
            var result = new List<ClassStudentAttendanceSummaryDto>();
            foreach (var s in students)
            {
                var pct = await GetStudentAttendancePercentageAsync(s.Id, periodType);
                if (pct >= threshold) continue;
                var summary = await GetStudentAttendanceSummaryAsync(s.Id, periodType, null, null);
                var consecutive = await GetStudentConsecutiveAbsenceDaysAsync(s.Id);
                result.Add(new ClassStudentAttendanceSummaryDto
                {
                    StudentId = s.Id,
                    StudentName = $"{s.FirstName} {s.LastName}".Trim(),
                    RollNumber = s.RollNumber,
                    AttendancePercentage = pct,
                    PresentCount = summary.PresentCount,
                    AbsentCount = summary.AbsentCount,
                    LeaveCount = summary.LeaveCount,
                    HalfDayCount = summary.HalfDayCount,
                    ConsecutiveAbsenceDays = consecutive
                });
            }
            return result.OrderBy(x => x.AttendancePercentage).ToList();
        }

        public async Task<IEnumerable<ClassStudentAttendanceSummaryDto>> GetClassChronicAbsenteesAsync(Guid classId, int consecutiveDays)
        {
            var students = await _studentRepository.FindAllAsync(s => s.ClassId == classId);
            if (students == null || students.Count == 0) return Array.Empty<ClassStudentAttendanceSummaryDto>();
            var result = new List<ClassStudentAttendanceSummaryDto>();
            foreach (var s in students)
            {
                var consecutive = await GetStudentConsecutiveAbsenceDaysAsync(s.Id);
                if (consecutive < consecutiveDays) continue;
                var summary = await GetStudentAttendanceSummaryAsync(s.Id, AttendancePeriodType.AcademicYear, null, null);
                result.Add(new ClassStudentAttendanceSummaryDto
                {
                    StudentId = s.Id,
                    StudentName = $"{s.FirstName} {s.LastName}".Trim(),
                    RollNumber = s.RollNumber,
                    AttendancePercentage = summary.Percentage,
                    PresentCount = summary.PresentCount,
                    AbsentCount = summary.AbsentCount,
                    LeaveCount = summary.LeaveCount,
                    HalfDayCount = summary.HalfDayCount,
                    ConsecutiveAbsenceDays = consecutive
                });
            }
            return result.OrderByDescending(x => x.ConsecutiveAbsenceDays).ToList();
        }

        public async Task<IEnumerable<AttendanceTrendPointDto>> GetClassAttendanceTrendAsync(Guid classId, AttendancePeriodType periodType)
        {
            var range = await ResolveDateRangeAsync(periodType, null, null, null);
            var records = await GetClassAttendanceRecordsInRangeAsync(classId, range.StartDate, range.EndDate);
            var branchId = await ResolveBranchIdForClassAsync(classId);
            var ctx = await ResolveCalendarAsync(branchId, range.StartDate, range.EndDate);
            var byDate = records
                .Where(a => a.AttendanceDate.HasValue)
                .GroupBy(a => a.AttendanceDate!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());
            var list = new List<AttendanceTrendPointDto>();
            for (var d = range.StartDate; d <= range.EndDate; d = d.AddDays(1))
            {
                byDate.TryGetValue(d, out var dayList);
                list.Add(new AttendanceTrendPointDto { Date = d, Status = FormatClassDayTrendLabel(d, dayList, ctx) });
            }
            return list.OrderBy(x => x.Date).ToList();
        }

        public async Task<IEnumerable<MonthAttendanceComparisonDto>> CompareClassAttendanceAcrossMonthsAsync(Guid classId)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var branchId = await ResolveBranchIdForClassAsync(classId);
            var result = new List<MonthAttendanceComparisonDto>();
            for (var i = 0; i < 6; i++)
            {
                var monthStart = new DateOnly(today.Year, today.Month, 1).AddMonths(-i);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                var records = await GetClassAttendanceRecordsInRangeAsync(classId, monthStart, monthEnd);
                var ctx = await ResolveCalendarAsync(branchId, monthStart, monthEnd);
                var working = records.Where(r => CountsTowardWorkingDayStats(r, ctx)).ToList();
                var monthTally = AttendanceTally.Of(working.Select(r => r.Status));
                var total = monthTally.WorkingDays;
                var present = monthTally.Present;
                var pct = monthTally.Percentage;
                result.Add(new MonthAttendanceComparisonDto
                {
                    Year = monthStart.Year,
                    Month = monthStart.Month,
                    MonthLabel = monthStart.ToString("MMM yyyy"),
                    Percentage = pct,
                    PresentCount = present,
                    TotalDays = total
                });
            }
            return result.OrderBy(x => x.Year).ThenBy(x => x.Month).ToList();
        }

        public async Task<DropoutRiskDto> PredictDropoutRiskForClassAsync(Guid classId)
        {
            var students = await _studentRepository.FindAllAsync(s => s.ClassId == classId);
            if (students == null || students.Count == 0)
                return new DropoutRiskDto { RiskLevel = "Low", RiskScore = 0, Factors = new List<string>(), StudentsAtRiskCount = 0 };

            var atRisk = new List<ClassStudentAttendanceSummaryDto>();
            var factors = new List<string>();
            foreach (var s in students)
            {
                var pct = await GetStudentAttendancePercentageAsync(s.Id, AttendancePeriodType.AcademicYear);
                var consecutive = await GetStudentConsecutiveAbsenceDaysAsync(s.Id);
                if (pct < 75 || consecutive >= 5)
                {
                    var summary = await GetStudentAttendanceSummaryAsync(s.Id, AttendancePeriodType.AcademicYear, null, null);
                    atRisk.Add(new ClassStudentAttendanceSummaryDto
                    {
                        StudentId = s.Id,
                        StudentName = $"{s.FirstName} {s.LastName}".Trim(),
                        RollNumber = s.RollNumber,
                        AttendancePercentage = pct,
                        PresentCount = summary.PresentCount,
                        AbsentCount = summary.AbsentCount,
                        LeaveCount = summary.LeaveCount,
                        HalfDayCount = summary.HalfDayCount,
                        ConsecutiveAbsenceDays = consecutive
                    });
                }
            }
            if (atRisk.Count > 0 && !factors.Contains("Low attendance students"))
                factors.Add("Low attendance students");
            if (atRisk.Any(x => x.ConsecutiveAbsenceDays >= 5) && !factors.Contains("Chronic absenteeism"))
                factors.Add("Chronic absenteeism");

            var riskRatio = (decimal)atRisk.Count / students.Count;
            decimal score = riskRatio * 100;
            string level = score >= 50 ? "Critical" : score >= 30 ? "High" : score >= 15 ? "Medium" : "Low";
            return new DropoutRiskDto
            {
                RiskLevel = level,
                RiskScore = Math.Round(score, 2),
                Factors = factors,
                StudentsAtRiskCount = atRisk.Count,
                StudentsAtRisk = atRisk
            };
        }

        public async Task<AttendanceVsResultDto> AttendanceVsResultAnalysisForClassAsync(Guid classId)
        {
            var classSummary = await GetClassAttendanceSummaryAsync(classId, AttendancePeriodType.AcademicYear, null, null);
            var students = await _studentRepository.FindAllAsync(s => s.ClassId == classId);
            var perStudent = new List<StudentAttendanceVsResultDto>();
            decimal? sumResult = null;
            var resultCount = 0;
            if (students != null)
            {
                foreach (var s in students)
                {
                    var attPct = await GetStudentAttendancePercentageAsync(s.Id, AttendancePeriodType.AcademicYear);
                    var results = await _studentResultRepository.FindAllAsync(r => r.StudentId == s.Id);
                    var latest = results?.OrderByDescending(r => r.GeneratedAt).FirstOrDefault();
                    decimal? resPct = latest?.Percentage;
                    if (resPct.HasValue) { sumResult = (sumResult ?? 0) + resPct.Value; resultCount++; }
                    perStudent.Add(new StudentAttendanceVsResultDto
                    {
                        StudentId = s.Id,
                        StudentName = $"{s.FirstName} {s.LastName}".Trim(),
                        AttendancePercentage = attPct,
                        ResultPercentage = resPct,
                        Grade = latest?.Grade
                    });
                }
            }
            var classAvgResult = resultCount > 0 && sumResult.HasValue ? Math.Round(sumResult.Value / resultCount, 2) : (decimal?)null;
            var correlationNote = classAvgResult.HasValue
                ? (classSummary.Percentage >= 80 && classAvgResult >= 60 ? "Strong positive: good attendance correlates with better results."
                  : classSummary.Percentage < 70 && classAvgResult < 50 ? "At-risk: low attendance correlates with lower results."
                  : "Moderate correlation between attendance and results.")
                : "Insufficient result data for correlation.";
            return new AttendanceVsResultDto
            {
                ClassAttendancePercentage = classSummary.Percentage,
                ClassAverageResultPercentage = classAvgResult,
                CorrelationNote = correlationNote,
                PerStudent = perStudent.OrderBy(p => p.StudentName).ToList()
            };
        }

        public async Task<IEnumerable<ClassStudentAttendanceSummaryDto>> GetTopRegularStudentsInClassAsync(Guid classId)
        {
            var students = await _studentRepository.FindAllAsync(s => s.ClassId == classId);
            if (students == null || students.Count == 0) return Array.Empty<ClassStudentAttendanceSummaryDto>();
            var list = new List<ClassStudentAttendanceSummaryDto>();
            foreach (var s in students)
            {
                var summary = await GetStudentAttendanceSummaryAsync(s.Id, AttendancePeriodType.AcademicYear, null, null);
                var consecutive = await GetStudentConsecutiveAbsenceDaysAsync(s.Id);
                list.Add(new ClassStudentAttendanceSummaryDto
                {
                    StudentId = s.Id,
                    StudentName = $"{s.FirstName} {s.LastName}".Trim(),
                    RollNumber = s.RollNumber,
                    AttendancePercentage = summary.Percentage,
                    PresentCount = summary.PresentCount,
                    AbsentCount = summary.AbsentCount,
                    LeaveCount = summary.LeaveCount,
                    HalfDayCount = summary.HalfDayCount,
                    ConsecutiveAbsenceDays = consecutive
                });
            }
            return list.OrderByDescending(x => x.AttendancePercentage).ToList();
        }

        public async Task<ClassAttendanceHeatmapDto> GetClassAttendanceHeatmapAsync(Guid classId)
        {
            var range = await ResolveDateRangeAsync(AttendancePeriodType.Month, null, null, null);
            var branchId = await ResolveBranchIdForClassAsync(classId);
            var ctx = await ResolveCalendarAsync(branchId, range.StartDate, range.EndDate);
            var students = await _studentRepository.FindAllAsync(s => s.ClassId == classId);
            if (students == null) students = new List<Student>();
            var records = await GetClassAttendanceRecordsInRangeAsync(classId, range.StartDate, range.EndDate);
            var byStudentAndDate = records
                .Where(a => a.StudentId.HasValue && a.AttendanceDate.HasValue)
                .GroupBy(a => a.StudentId!.Value)
                .ToDictionary(g => g.Key, g => g.ToDictionary(x => x.AttendanceDate!.Value, x => x.Status ?? "N/A"));

            var dates = new List<DateOnly>();
            for (var d = range.StartDate; d <= range.EndDate; d = d.AddDays(1))
                dates.Add(d);

            var studentList = students.OrderBy(s => s.RollNumber).ThenBy(s => s.FirstName).ToList();
            var grid = new List<List<string>>();
            foreach (var s in studentList)
            {
                var row = new List<string>();
                foreach (var date in dates)
                {
                    var nw = NonWorkingCalendarCell(date, ctx);
                    if (nw != null)
                    {
                        row.Add(nw);
                        continue;
                    }
                    if (byStudentAndDate.TryGetValue(s.Id, out var byDate) && byDate.TryGetValue(date, out var status))
                        row.Add(status);
                    else
                        row.Add("—");
                }
                grid.Add(row);
            }
            return new ClassAttendanceHeatmapDto
            {
                StartDate = range.StartDate,
                EndDate = range.EndDate,
                Dates = dates,
                Students = studentList.Select(s => new HeatmapStudentInfoDto
                {
                    StudentId = s.Id,
                    StudentName = $"{s.FirstName} {s.LastName}".Trim(),
                    RollNumber = s.RollNumber
                }).ToList(),
                StatusGrid = grid
            };
        }

        // --- Division ---
        public async Task<AttendanceReportDto> GetDivisionAttendanceReportAsync(Guid divisionId, AttendancePeriodType periodType, DateTime? startDate, DateTime? endDate)
        {
            var range = await ResolveDateRangeAsync(periodType, startDate, endDate, null);
            var summary = await GetDivisionAttendanceSummaryAsync(divisionId, periodType, startDate, endDate);
            var list = await GetDivisionAttendanceDetailedListAsync(divisionId, periodType, startDate, endDate);
            return new AttendanceReportDto { PeriodType = periodType, StartDate = range.StartDate, EndDate = range.EndDate, Summary = summary, DetailedList = list };
        }

        public async Task<AttendanceSummaryDto> GetDivisionAttendanceSummaryAsync(Guid divisionId, AttendancePeriodType periodType, DateTime? startDate, DateTime? endDate)
        {
            var range = await ResolveDateRangeAsync(periodType, startDate, endDate, null);
            var records = await GetDivisionAttendanceRecordsInRangeAsync(divisionId, range.StartDate, range.EndDate);
            var branchId = await ResolveBranchIdForDivisionAsync(divisionId);
            var ctx = await ResolveCalendarAsync(branchId, range.StartDate, range.EndDate);
            return BuildSummaryFromRecords(records, ctx);
        }

        public async Task<IEnumerable<AttendanceResponse>> GetDivisionAttendanceDetailedListAsync(Guid divisionId, AttendancePeriodType periodType, DateTime? startDate, DateTime? endDate)
        {
            var range = await ResolveDateRangeAsync(periodType, startDate, endDate, null);
            var records = await GetDivisionAttendanceRecordsInRangeAsync(divisionId, range.StartDate, range.EndDate);
            return records.Select(_mapper.Map<AttendanceResponse>).OrderBy(r => r.AttendanceDate).ThenBy(r => r.StudentId).ToList();
        }

        public async Task<decimal> GetDivisionAttendancePercentageAsync(Guid divisionId, AttendancePeriodType periodType)
        {
            var summary = await GetDivisionAttendanceSummaryAsync(divisionId, periodType, null, null);
            return summary.Percentage;
        }

        public async Task<int> GetDivisionPresentCountAsync(Guid divisionId, AttendancePeriodType periodType)
        {
            var range = await ResolveDateRangeAsync(periodType, null, null, null);
            var records = await GetDivisionAttendanceRecordsInRangeAsync(divisionId, range.StartDate, range.EndDate);
            var branchId = await ResolveBranchIdForDivisionAsync(divisionId);
            var ctx = await ResolveCalendarAsync(branchId, range.StartDate, range.EndDate);
            return records.Count(r => CountsTowardWorkingDayStats(r, ctx) && IsStatus(r.Status, "Present"));
        }

        public async Task<int> GetDivisionAbsentCountAsync(Guid divisionId, AttendancePeriodType periodType)
        {
            var range = await ResolveDateRangeAsync(periodType, null, null, null);
            var records = await GetDivisionAttendanceRecordsInRangeAsync(divisionId, range.StartDate, range.EndDate);
            var branchId = await ResolveBranchIdForDivisionAsync(divisionId);
            var ctx = await ResolveCalendarAsync(branchId, range.StartDate, range.EndDate);
            return records.Count(r => CountsTowardWorkingDayStats(r, ctx) && IsStatus(r.Status, "Absent"));
        }

        public async Task<int> GetDivisionLeaveCountAsync(Guid divisionId, AttendancePeriodType periodType)
        {
            var range = await ResolveDateRangeAsync(periodType, null, null, null);
            var records = await GetDivisionAttendanceRecordsInRangeAsync(divisionId, range.StartDate, range.EndDate);
            var branchId = await ResolveBranchIdForDivisionAsync(divisionId);
            var ctx = await ResolveCalendarAsync(branchId, range.StartDate, range.EndDate);
            return records.Count(r => CountsTowardWorkingDayStats(r, ctx) && IsStatus(r.Status, "Leave"));
        }

        public async Task<IEnumerable<ClassStudentAttendanceSummaryDto>> GetDivisionLowAttendanceStudentsAsync(Guid divisionId, decimal threshold)
        {
            var students = await _studentRepository.FindAllAsync(s => s.DivisionId == divisionId);
            if (students == null || students.Count == 0) return Array.Empty<ClassStudentAttendanceSummaryDto>();
            var result = new List<ClassStudentAttendanceSummaryDto>();
            foreach (var s in students)
            {
                var pct = await GetStudentAttendancePercentageAsync(s.Id, AttendancePeriodType.AcademicYear);
                if (pct >= threshold) continue;
                var summary = await GetStudentAttendanceSummaryAsync(s.Id, AttendancePeriodType.AcademicYear, null, null);
                result.Add(new ClassStudentAttendanceSummaryDto
                {
                    StudentId = s.Id, StudentName = $"{s.FirstName} {s.LastName}".Trim(), RollNumber = s.RollNumber,
                    AttendancePercentage = pct, PresentCount = summary.PresentCount, AbsentCount = summary.AbsentCount,
                    LeaveCount = summary.LeaveCount, HalfDayCount = summary.HalfDayCount,
                    ConsecutiveAbsenceDays = await GetStudentConsecutiveAbsenceDaysAsync(s.Id)
                });
            }
            return result.OrderBy(x => x.AttendancePercentage).ToList();
        }

        public async Task<IEnumerable<ClassStudentAttendanceSummaryDto>> GetDivisionChronicAbsenteesAsync(Guid divisionId)
        {
            var students = await _studentRepository.FindAllAsync(s => s.DivisionId == divisionId);
            if (students == null || students.Count == 0) return Array.Empty<ClassStudentAttendanceSummaryDto>();
            var result = new List<ClassStudentAttendanceSummaryDto>();
            foreach (var s in students)
            {
                var consecutive = await GetStudentConsecutiveAbsenceDaysAsync(s.Id);
                if (consecutive < 5) continue;
                var summary = await GetStudentAttendanceSummaryAsync(s.Id, AttendancePeriodType.AcademicYear, null, null);
                result.Add(new ClassStudentAttendanceSummaryDto
                {
                    StudentId = s.Id, StudentName = $"{s.FirstName} {s.LastName}".Trim(), RollNumber = s.RollNumber,
                    AttendancePercentage = summary.Percentage, PresentCount = summary.PresentCount, AbsentCount = summary.AbsentCount,
                    LeaveCount = summary.LeaveCount, HalfDayCount = summary.HalfDayCount, ConsecutiveAbsenceDays = consecutive
                });
            }
            return result.OrderByDescending(x => x.ConsecutiveAbsenceDays).ToList();
        }

        public async Task<IEnumerable<AttendanceTrendPointDto>> GetDivisionAttendanceTrendAsync(Guid divisionId, AttendancePeriodType periodType)
        {
            var range = await ResolveDateRangeAsync(periodType, null, null, null);
            var records = await GetDivisionAttendanceRecordsInRangeAsync(divisionId, range.StartDate, range.EndDate);
            var branchId = await ResolveBranchIdForDivisionAsync(divisionId);
            var ctx = await ResolveCalendarAsync(branchId, range.StartDate, range.EndDate);
            var byDate = records
                .Where(a => a.AttendanceDate.HasValue)
                .GroupBy(a => a.AttendanceDate!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());
            var list = new List<AttendanceTrendPointDto>();
            for (var d = range.StartDate; d <= range.EndDate; d = d.AddDays(1))
            {
                byDate.TryGetValue(d, out var dayList);
                list.Add(new AttendanceTrendPointDto { Date = d, Status = FormatClassDayTrendLabel(d, dayList, ctx) });
            }
            return list.OrderBy(x => x.Date).ToList();
        }

        // --- Branch ---
        public async Task<AttendanceReportDto> GetBranchAttendanceReportAsync(Guid branchId, AttendancePeriodType periodType, DateTime? startDate, DateTime? endDate)
        {
            var range = await ResolveDateRangeAsync(periodType, startDate, endDate, null);
            var summary = await GetBranchAttendanceSummaryAsync(branchId, periodType);
            var records = await GetBranchAttendanceRecordsInRangeAsync(branchId, range.StartDate, range.EndDate);
            var list = records.Select(_mapper.Map<AttendanceResponse>).OrderBy(r => r.AttendanceDate).ThenBy(r => r.StudentId).ToList();
            return new AttendanceReportDto { PeriodType = periodType, StartDate = range.StartDate, EndDate = range.EndDate, Summary = summary, DetailedList = list };
        }

        public async Task<AttendanceSummaryDto> GetBranchAttendanceSummaryAsync(Guid branchId, AttendancePeriodType periodType)
        {
            var range = await ResolveDateRangeAsync(periodType, null, null, null);
            var records = await GetBranchAttendanceRecordsInRangeAsync(branchId, range.StartDate, range.EndDate);
            var ctx = await ResolveCalendarAsync(branchId, range.StartDate, range.EndDate);
            return BuildSummaryFromRecords(records, ctx);
        }

        public async Task<decimal> GetBranchAttendancePercentageAsync(Guid branchId, AttendancePeriodType periodType)
        {
            var summary = await GetBranchAttendanceSummaryAsync(branchId, periodType);
            return summary.Percentage;
        }

        public async Task<IEnumerable<ClassStudentAttendanceSummaryDto>> GetBranchLowAttendanceStudentsAsync(Guid branchId, decimal threshold)
        {
            var students = await _studentRepository.FindAllAsync(s => s.BranchId == branchId);
            if (students == null || students.Count == 0) return Array.Empty<ClassStudentAttendanceSummaryDto>();
            var result = new List<ClassStudentAttendanceSummaryDto>();
            foreach (var s in students)
            {
                var pct = await GetStudentAttendancePercentageAsync(s.Id, AttendancePeriodType.AcademicYear);
                if (pct >= threshold) continue;
                var summary = await GetStudentAttendanceSummaryAsync(s.Id, AttendancePeriodType.AcademicYear, null, null);
                result.Add(new ClassStudentAttendanceSummaryDto
                {
                    StudentId = s.Id, StudentName = $"{s.FirstName} {s.LastName}".Trim(), RollNumber = s.RollNumber,
                    AttendancePercentage = pct, PresentCount = summary.PresentCount, AbsentCount = summary.AbsentCount,
                    LeaveCount = summary.LeaveCount, HalfDayCount = summary.HalfDayCount,
                    ConsecutiveAbsenceDays = await GetStudentConsecutiveAbsenceDaysAsync(s.Id)
                });
            }
            return result.OrderBy(x => x.AttendancePercentage).ToList();
        }

        public async Task<IEnumerable<ClassStudentAttendanceSummaryDto>> GetBranchChronicAbsenteesAsync(Guid branchId)
        {
            var students = await _studentRepository.FindAllAsync(s => s.BranchId == branchId);
            if (students == null || students.Count == 0) return Array.Empty<ClassStudentAttendanceSummaryDto>();
            var result = new List<ClassStudentAttendanceSummaryDto>();
            foreach (var s in students)
            {
                var consecutive = await GetStudentConsecutiveAbsenceDaysAsync(s.Id);
                if (consecutive < 5) continue;
                var summary = await GetStudentAttendanceSummaryAsync(s.Id, AttendancePeriodType.AcademicYear, null, null);
                result.Add(new ClassStudentAttendanceSummaryDto
                {
                    StudentId = s.Id, StudentName = $"{s.FirstName} {s.LastName}".Trim(), RollNumber = s.RollNumber,
                    AttendancePercentage = summary.Percentage, PresentCount = summary.PresentCount, AbsentCount = summary.AbsentCount,
                    LeaveCount = summary.LeaveCount, HalfDayCount = summary.HalfDayCount, ConsecutiveAbsenceDays = consecutive
                });
            }
            return result.OrderByDescending(x => x.ConsecutiveAbsenceDays).ToList();
        }

        public async Task<IEnumerable<AttendanceTrendPointDto>> GetBranchAttendanceTrendAsync(Guid branchId, AttendancePeriodType periodType)
        {
            var range = await ResolveDateRangeAsync(periodType, null, null, null);
            var records = await GetBranchAttendanceRecordsInRangeAsync(branchId, range.StartDate, range.EndDate);
            var ctx = await ResolveCalendarAsync(branchId, range.StartDate, range.EndDate);
            var byDate = records
                .Where(a => a.AttendanceDate.HasValue)
                .GroupBy(a => a.AttendanceDate!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());
            var list = new List<AttendanceTrendPointDto>();
            for (var d = range.StartDate; d <= range.EndDate; d = d.AddDays(1))
            {
                byDate.TryGetValue(d, out var dayList);
                list.Add(new AttendanceTrendPointDto { Date = d, Status = FormatClassDayTrendLabel(d, dayList, ctx) });
            }
            return list.OrderBy(x => x.Date).ToList();
        }

        public async Task<IEnumerable<ClassAttendanceComparisonDto>> AttendanceComparisonBetweenClassesAsync(Guid branchId, AttendancePeriodType periodType)
        {
            var classes = await _studentRepository.FindAllAsync(s => s.BranchId == branchId);
            var classIds = classes?.Select(s => s.ClassId).Where(c => c.HasValue).Select(c => c!.Value).Distinct().ToList() ?? new List<Guid>();
            var result = new List<ClassAttendanceComparisonDto>();
            foreach (var cid in classIds)
            {
                var pct = await GetClassAttendancePercentageAsync(cid, periodType);
                var range = await ResolveDateRangeAsync(periodType, null, null, null);
                var records = await GetClassAttendanceRecordsInRangeAsync(cid, range.StartDate, range.EndDate);
                var ctx = await ResolveCalendarAsync(branchId, range.StartDate, range.EndDate);
                var working = records.Where(r => CountsTowardWorkingDayStats(r, ctx)).ToList();
                var total = working.Count;
                var present = working.Count(r => IsStatus(r.Status, "Present"));
                result.Add(new ClassAttendanceComparisonDto { ClassId = cid, ClassName = null, AttendancePercentage = pct, PresentCount = present, TotalCount = total });
            }
            return result.OrderByDescending(x => x.AttendancePercentage).ToList();
        }

        public async Task<IEnumerable<GenderWiseAttendanceDistributionDto>> GenderWiseAttendanceDistributionAsync(Guid branchId, AttendancePeriodType periodType)
        {
            var students = await _studentRepository.FindAllAsync(s => s.BranchId == branchId);
            if (students == null) return Array.Empty<GenderWiseAttendanceDistributionDto>();
            var byGender = students.GroupBy(s => s.Gender ?? "Unknown").ToList();
            var result = new List<GenderWiseAttendanceDistributionDto>();
            foreach (var g in byGender)
            {
                var ids = g.Select(s => s.Id).ToHashSet();
                decimal pct = 0, attended = 0; int present = 0; int total = 0;
                foreach (var s in g)
                {
                    var summary = await GetStudentAttendanceSummaryAsync(s.Id, periodType, null, null);
                    total += summary.TotalDays; present += summary.PresentCount;
                    attended += summary.Percentage * summary.TotalDays / 100m;
                }
                if (total > 0) pct = Math.Round(attended / total * 100, 2);
                result.Add(new GenderWiseAttendanceDistributionDto { Gender = g.Key, StudentCount = g.Count(), AttendancePercentage = pct, PresentCount = present, TotalRecords = total });
            }
            return result;
        }

        public async Task<IEnumerable<CategoryWiseAttendanceDistributionDto>> CategoryWiseAttendanceDistributionAsync(Guid branchId, AttendancePeriodType periodType)
        {
            var students = await _studentRepository.FindAllAsync(s => s.BranchId == branchId);
            if (students == null) return Array.Empty<CategoryWiseAttendanceDistributionDto>();
            var byCat = students.GroupBy(s => s.Category ?? "General").ToList();
            var result = new List<CategoryWiseAttendanceDistributionDto>();
            foreach (var g in byCat)
            {
                int present = 0, total = 0;
                decimal attended = 0;
                foreach (var s in g)
                {
                    var summary = await GetStudentAttendanceSummaryAsync(s.Id, periodType, null, null);
                    total += summary.TotalDays; present += summary.PresentCount;
                    attended += summary.Percentage * summary.TotalDays / 100m;
                }
                var pct = total > 0 ? Math.Round(attended / total * 100, 2) : 0;
                result.Add(new CategoryWiseAttendanceDistributionDto { Category = g.Key, StudentCount = g.Count(), AttendancePercentage = pct, PresentCount = present, TotalRecords = total });
            }
            return result;
        }

        // --- Today & Dashboard ---
        public async Task<AttendanceSummaryDto> GetTodayAttendanceSummaryAsync(Guid branchId)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var records = await GetBranchAttendanceRecordsInRangeAsync(branchId, today, today);
            var ctx = await ResolveCalendarAsync(branchId, today, today);
            return BuildSummaryFromRecords(records, ctx);
        }

        public async Task<int> GetTodayPresentCountAsync(Guid branchId)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var records = await GetBranchAttendanceRecordsInRangeAsync(branchId, today, today);
            var ctx = await ResolveCalendarAsync(branchId, today, today);
            return records.Count(r => CountsTowardWorkingDayStats(r, ctx) && IsStatus(r.Status, "Present"));
        }

        public async Task<int> GetTodayAbsentCountAsync(Guid branchId)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var records = await GetBranchAttendanceRecordsInRangeAsync(branchId, today, today);
            var ctx = await ResolveCalendarAsync(branchId, today, today);
            return records.Count(r => CountsTowardWorkingDayStats(r, ctx) && IsStatus(r.Status, "Absent"));
        }

        public async Task<decimal> GetTodayAttendancePercentageAsync(Guid branchId)
        {
            var summary = await GetTodayAttendanceSummaryAsync(branchId);
            return summary.Percentage;
        }

        public async Task<AttendanceDashboardDto> GetAttendanceDashboardDataAsync(Guid branchId)
        {
            var cacheKey = $"attendance_dashboard_{branchId}";
            var cached = _cache.GetCache<AttendanceDashboardDto>(cacheKey);
            if (cached != null) return cached;
            var todaySummary = await GetTodayAttendanceSummaryAsync(branchId);
            var students = await _studentRepository.FindAllAsync(s => s.BranchId == branchId);
            var monthSummary = await GetBranchAttendanceSummaryAsync(branchId, AttendancePeriodType.Month);
            var pendingLeave = await GetPendingLeaveApprovalsForBranchAsync(branchId);
            var pendingCorrection = await GetPendingCorrectionCountForBranchAsync(branchId);
            var dto = new AttendanceDashboardDto
            {
                TodayPresent = todaySummary.PresentCount, TodayAbsent = todaySummary.AbsentCount, TodayLeave = todaySummary.LeaveCount,
                TodayPercentage = todaySummary.Percentage, TotalStudents = students?.Count ?? 0, MonthSummary = monthSummary,
                PendingLeaveApprovals = pendingLeave, PendingCorrectionRequests = pendingCorrection
            };
            _cache.SetCache(cacheKey, dto, TimeSpan.FromMinutes(1));
            return dto;
        }

        public async Task<IEnumerable<LeaveRequestDto>> GetStudentLeaveHistoryAsync(Guid studentId)
        {
            var list = await _leaveRequestRepository.FindAllAsync(l => l.StudentId == studentId);
            var student = await _studentRepository.GetByIdAsync(studentId);
            return (list ?? new List<LeaveRequest>()).OrderByDescending(l => l.RequestedAt).Select(l => MapToLeaveRequestDto(l, student)).ToList();
        }

        public async Task<IEnumerable<LeaveRequestDto>> GetPendingLeaveApprovalsAsync(Guid classId)
        {
            var students = await _studentRepository.FindAllAsync(s => s.ClassId == classId);
            if (students == null || students.Count == 0) return Array.Empty<LeaveRequestDto>();
            var ids = students.Select(s => s.Id).ToHashSet();
            var list = await _leaveRequestRepository.FindAllAsync(l => l.Status == "Pending" && ids.Contains(l.StudentId));
            return (list ?? new List<LeaveRequest>()).Select(l => MapToLeaveRequestDto(l, students.FirstOrDefault(s => s.Id == l.StudentId))).ToList();
        }

        public async Task<IEnumerable<AttendanceCorrectionRequestDto>> GetAttendanceCorrectionRequestsAsync(Guid classId)
        {
            var students = await _studentRepository.FindAllAsync(s => s.ClassId == classId);
            if (students == null || students.Count == 0) return Array.Empty<AttendanceCorrectionRequestDto>();
            var studentIds = students.Select(s => s.Id).ToHashSet();
            var attendances = await _attendanceRepository.FindAllAsync(a => a.StudentId.HasValue && studentIds.Contains(a.StudentId.Value));
            var attIds = (attendances ?? new List<Attendance>()).Select(a => a.Id).ToHashSet();
            var list = await _correctionRequestRepository.FindAllAsync(c => attIds.Contains(c.AttendanceId));
            return (list ?? new List<AttendanceCorrectionRequest>()).Select(c => new AttendanceCorrectionRequestDto
            {
                AttendanceId = c.AttendanceId, Reason = c.Reason, Status = c.Status,
                RequestedAt = c.RequestedAt, RequestedBy = c.RequestedBy, ApprovedAt = c.ApprovedAt, ApprovedBy = c.ApprovedBy, RejectionReason = c.RejectionReason
            }.WithAuditFrom(c)).ToList();
        }

        // --- Export (simple CSV placeholder) ---
        public async Task<ExportResultDto> ExportStudentAttendanceReportAsync(Guid studentId, AttendancePeriodType periodType)
        {
            var list = await GetStudentAttendanceDetailedListAsync(studentId, periodType, null, null);
            return BuildCsvExport("student_attendance.csv", list.Select(a => new { a.AttendanceDate, a.Status, a.LeaveType, a.Remark }));
        }

        public async Task<ExportResultDto> ExportClassAttendanceReportAsync(Guid classId, AttendancePeriodType periodType)
        {
            var list = await GetClassAttendanceDetailedListAsync(classId, periodType, null, null);
            return BuildCsvExport("class_attendance.csv", list.Select(a => new { a.StudentId, a.AttendanceDate, a.Status, a.LeaveType }));
        }

        public async Task<ExportResultDto> ExportDivisionAttendanceReportAsync(Guid divisionId, AttendancePeriodType periodType)
        {
            var list = await GetDivisionAttendanceDetailedListAsync(divisionId, periodType, null, null);
            return BuildCsvExport("division_attendance.csv", list.Select(a => new { a.StudentId, a.AttendanceDate, a.Status }));
        }

        public async Task<ExportResultDto> ExportBranchAttendanceReportAsync(Guid branchId, AttendancePeriodType periodType)
        {
            var range = await ResolveDateRangeAsync(periodType, null, null, null);
            var records = await GetBranchAttendanceRecordsInRangeAsync(branchId, range.StartDate, range.EndDate);
            var list = records.Select(_mapper.Map<AttendanceResponse>);
            return BuildCsvExport("branch_attendance.csv", list.Select(a => new { a.StudentId, a.AttendanceDate, a.Status }));
        }

        public async Task<ExportResultDto> GenerateAttendanceRegisterPDFAsync(Guid classId, int month, int year)
        {
            var start = new DateOnly(year, month, 1);
            var end = start.AddMonths(1).AddDays(-1);
            var list = await GetClassAttendanceDetailedListAsync(classId, AttendancePeriodType.Custom, start.ToDateTime(TimeOnly.MinValue), end.ToDateTime(TimeOnly.MinValue));
            var csv = BuildCsvExport($"register_{classId}_{year}_{month}.csv", list.Select(a => new { a.StudentId, a.AttendanceDate, a.Status }));
            return new ExportResultDto { Content = csv.Content, FileName = csv.FileName.Replace(".csv", ".pdf"), ContentType = "application/pdf" };
        }

        public async Task<ExportResultDto> GenerateAttendanceExcelAsync(Guid branchId, AttendancePeriodType periodType)
        {
            var list = await ExportBranchAttendanceReportAsync(branchId, periodType);
            return new ExportResultDto { Content = list.Content, FileName = list.FileName.Replace(".csv", ".xlsx"), ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" };
        }

        // --- UDISE / RTE ---
        public async Task<UDISEReportDto> GetUDISEAttendanceReportAsync(Guid branchId)
        {
            var students = await _studentRepository.FindAllAsync(s => s.BranchId == branchId);
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var records = await GetBranchAttendanceRecordsInRangeAsync(branchId, today, today);
            var ctx = await ResolveCalendarAsync(branchId, today, today);
            var todayTally = AttendanceTally.Of(records.Where(r => CountsTowardWorkingDayStats(r, ctx)).Select(r => r.Status));
            var present = todayTally.Present + todayTally.Late;
            var absent = todayTally.Absent;
            var total = students?.Count ?? 0;
            var rate = total > 0 ? Math.Round(todayTally.Attended / total * 100, 2) : 0;
            var rteStudents = students?.Count(s => s.IsRteSeat == true) ?? 0;
            var rteIds = students?.Where(s => s.IsRteSeat == true).Select(s => s.Id).ToHashSet() ?? new HashSet<Guid>();
            var rtePresent = records.Count(r => r.StudentId.HasValue && rteIds.Contains(r.StudentId.Value) && CountsTowardWorkingDayStats(r, ctx) && IsStatus(r.Status, "Present"));
            var rteRate = rteStudents > 0 ? Math.Round((decimal)rtePresent / rteStudents * 100, 2) : 0;
            return new UDISEReportDto { BranchId = branchId, TotalEnrollment = total, PresentToday = present, AbsentToday = absent, AttendanceRate = rate, RteStudents = rteStudents, RtePresent = rtePresent, RteAttendanceRate = rteRate };
        }

        public async Task<RTEComplianceDto> GetRTEAttendanceComplianceAsync(Guid branchId)
        {
            var students = await _studentRepository.FindAllAsync(s => s.BranchId == branchId);
            var rteCount = students?.Count(s => s.IsRteSeat == true) ?? 0;
            if (rteCount == 0) return new RTEComplianceDto { BranchId = branchId, RteSeatCount = 0, RteEnrolled = 0, AttendanceCompliancePercentage = 100, IsCompliant = true, Notes = "No RTE students." };
            decimal pct = 0;
            foreach (var s in students!.Where(s => s.IsRteSeat == true))
                pct += await GetStudentAttendancePercentageAsync(s.Id, AttendancePeriodType.AcademicYear);
            var avg = rteCount > 0 ? Math.Round(pct / rteCount, 2) : 0;
            return new RTEComplianceDto { BranchId = branchId, RteSeatCount = rteCount, RteEnrolled = rteCount, AttendanceCompliancePercentage = avg, IsCompliant = avg >= 75, Notes = avg >= 75 ? "Compliant" : "Below 75%" };
        }
    }
}
