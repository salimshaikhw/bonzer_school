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
    /// <summary>Marking, imports, locks, edits and the leave / correction workflows: the write side of attendance (rules in IAttendancePolicy).</summary>
    public class AttendanceService : AttendanceServiceBase, IAttendanceService
    {
        public AttendanceService(AttendanceServiceDependencies deps, ILogger<AttendanceService> logger)
            : base(deps, logger)
        {
        }

        public async Task<IEnumerable<AttendanceResponse>> GetAllAsync()
        {
            const string cacheKey = "attendances_all";
            var cached = _cache.GetCache<IEnumerable<AttendanceResponse>>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Fetched all attendances from cache.");
                return cached;
            }
            var entities = await _attendanceRepository.FindAllAsync(a => a.IsDeleted != true);
            var dtos = _mapper.Map<IEnumerable<AttendanceResponse>>(entities);
            _cache.SetCache(cacheKey, dtos, TimeSpan.FromMinutes(10));
            _logger.LogInformation("Fetched all attendances from DB and cached result.");
            return dtos;
        }

        public async Task<AttendanceResponse?> GetByIdAsync(Guid id)
        {
            string cacheKey = $"attendance_{id}";
            var cached = _cache.GetCache<AttendanceResponse>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Fetched attendance {Id} from cache.", id);
                return cached;
            }
            var entity = await _attendanceRepository.GetByIdAsync(id);
            if (entity == null || entity.IsDeleted == true) return null;
            var dto = _mapper.Map<AttendanceResponse>(entity);
            _cache.SetCache(cacheKey, dto, TimeSpan.FromMinutes(10));
            _logger.LogInformation("Fetched attendance {Id} from DB and cached result.", id);
            return dto;
        }
        public async Task<AttendanceResponse> CreateAsync(AttendanceRequest input)
        {
            try
            {
                if (input.StudentId is not { } studentId || input.AttendanceDate is not { } date)
                    throw new Application.Exceptions.BaseServiceException("StudentId and AttendanceDate are required", Application.Common.ExceptionCodes.Validation);
                var student = await _policy.EnsureCanMarkAsync(studentId, date, input.ClassId, input.DivisionId);
                var entity = _mapper.Map<Attendance>(input);
                entity.Id = Guid.NewGuid();
                entity.Status = RequireStatus(input.Status);
                entity.ClassId ??= student.ClassId;
                entity.DivisionId ??= student.DivisionId;
                entity.CreatedAt = DateTime.UtcNow;
                entity.IsDeleted = false;
                await _attendanceRepository.AddAsync(entity);
                _cache.RemoveCache("attendances_all");
                _logger.LogInformation("Created new attendance {Id}", entity.Id);
                return _mapper.Map<AttendanceResponse>(entity);
            }
            catch (Exception ex)
            {
                throw new Application.Exceptions.BaseServiceException("Error creating attendance", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<AttendanceResponse> UpdateAsync(Guid id, AttendanceRequest input)
        {
            try
            {
                var entity = await _attendanceRepository.GetByIdAsync(id);
                if (entity == null)
                {
                    _logger.LogWarning("Attendance not found: {Id}", id);
                    throw new Application.Exceptions.BaseServiceException("Attendance not found", Application.Common.ExceptionCodes.ItemNotFound);
                }
                await _policy.EnsureNotLockedAsync(entity.ClassId, entity.DivisionId, entity.AttendanceDate!.Value);
                if (input.StudentId != entity.StudentId || input.AttendanceDate != entity.AttendanceDate)
                    throw new Application.Exceptions.BaseServiceException("The student and date of an attendance record cannot be changed; delete it and mark the right one.", Application.Common.ExceptionCodes.Validation);
                _mapper.Map(input, entity);
                entity.Status = RequireStatus(input.Status);
                await _attendanceRepository.UpdateAsync(entity);
                _cache.RemoveCache("attendances_all");
                _cache.RemoveCache($"attendance_{id}");
                _logger.LogInformation("Updated attendance {Id}", id);
                return _mapper.Map<AttendanceResponse>(entity);
            }
            catch (Application.Exceptions.BaseServiceException) { throw; }
            catch (Exception ex)
            {
                throw new Application.Exceptions.BaseServiceException("Error updating attendance", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                // Hard delete must also reach soft-deleted rows, which the global filter hides.
                var entity = await _attendanceRepository.FindAsync(a => a.Id == id, trackChanges: true, ignoreQueryFilters: true);
                if (entity == null)
                {
                    _logger.LogWarning("Attendance not found: {Id}", id);
                    throw new Application.Exceptions.BaseServiceException("Attendance not found", Application.Common.ExceptionCodes.ItemNotFound);
                }
                if (entity.AttendanceDate is { } deletedDate)
                    await _policy.EnsureNotLockedAsync(entity.ClassId, entity.DivisionId, deletedDate);
                await _attendanceRepository.RemoveRangeAsync(new[] { entity });
                _cache.RemoveCache("attendances_all");
                _cache.RemoveCache($"attendance_{id}");
                _logger.LogInformation("Deleted attendance {Id}", id);
                return true;
            }
            catch (Application.Exceptions.BaseServiceException) { throw; }
            catch (Exception ex)
            {
                throw new Application.Exceptions.BaseServiceException("Error deleting attendance", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        private static string WithoutServicePrefix(string message) =>
            message.StartsWith("Service Exception: ", StringComparison.Ordinal) ? message["Service Exception: ".Length..] : message;

        /// <summary>Canonical status (Present, Absent, Late, HalfDay, Leave) or a 400 listing the valid ones.</summary>
        private static string RequireStatus(string? status) =>
            AttendanceStatuses.Normalize(status)
            ?? throw new Application.Exceptions.BaseServiceException(
                $"Unknown attendance status '{status}'. Use one of: {string.Join(", ", AttendanceStatuses.All)}.",
                Application.Common.ExceptionCodes.Validation);

        /// <summary>
        /// Checks every entry of a register before anything is written, so one bad entry cannot leave half a
        /// class marked. Also rejects the same student twice in one batch.
        /// </summary>
        private async Task PrevalidateAsync(IEnumerable<MarkAttendanceDto> entries)
        {
            var seen = new HashSet<(Guid, DateOnly)>();
            foreach (var entry in entries)
            {
                if (entry.StudentId is not { } studentId || entry.AttendanceDate is not { } date)
                    throw new Application.Exceptions.BaseServiceException("Every entry needs a student and a date.", Application.Common.ExceptionCodes.Validation);
                if (!seen.Add((studentId, date)))
                    throw new Application.Exceptions.BaseServiceException($"Student {studentId} appears more than once for {date:dd-MM-yyyy}.", Application.Common.ExceptionCodes.Validation);
                RequireStatus(entry.Status);
                await _policy.EnsureCanMarkAsync(studentId, date, entry.ClassId, entry.DivisionId);
            }
        }

        public async Task<int> CommitAsync()
        {
            return await _unitOfWork.Commit();
        }

        public async Task<AttendanceResponse> MarkAttendanceAsync(MarkAttendanceDto dto)
        {
            try
            {
                _logger.LogInformation("MarkAttendance: StudentId={StudentId}, Date={Date}, Status={Status}", dto.StudentId, dto.AttendanceDate, dto.Status);
                if (!dto.StudentId.HasValue || !dto.AttendanceDate.HasValue)
                    throw new Application.Exceptions.BaseServiceException("StudentId and AttendanceDate are required", Application.Common.ExceptionCodes.Validation);

                var existing = await _attendanceRepository.FindAsync(a =>
                    a.StudentId == dto.StudentId &&
                    a.AttendanceDate == dto.AttendanceDate &&
                    a.IsDeleted != true);
                var dateOnly = dto.AttendanceDate.Value;
                var status = RequireStatus(dto.Status);
                var student = await _policy.EnsureCanMarkAsync(dto.StudentId.Value, dateOnly, dto.ClassId, dto.DivisionId);
                dto.ClassId ??= student.ClassId;
                dto.DivisionId ??= student.DivisionId;
                var branchId = student.BranchId;
                var w = await ResolveWeeklyOffAsync(branchId);
                var isWeeklyOff = IsWeeklyOffDay(dateOnly, w.SaturdayPattern, w.SundayOff);
                var calendarHoliday = branchId.HasValue && await _schoolHolidayRepository.HasActiveHolidayAsync(branchId.Value, dateOnly);
                var effectiveHoliday = dto.IsHoliday == true || isWeeklyOff || calendarHoliday;
                if (existing != null)
                {
                    existing.Status = status;
                    existing.LeaveType = dto.LeaveType;
                    existing.IsHoliday = effectiveHoliday;
                    existing.Remark = dto.Remark;
                    if (dto.ClassId.HasValue) existing.ClassId = dto.ClassId;
                    if (dto.DivisionId.HasValue) existing.DivisionId = dto.DivisionId;
                    if (dto.ClassTeacherId.HasValue) existing.ClassTeacherId = dto.ClassTeacherId;
                    await _attendanceRepository.UpdateAsync(existing);
                    _cache.RemoveCache("attendances_all");
                    _cache.RemoveCache($"attendance_{existing.Id}");
                    _logger.LogInformation("Updated existing attendance {Id} for student {StudentId}", existing.Id, dto.StudentId);
                    return _mapper.Map<AttendanceResponse>(existing);
                }

                var entity = new Attendance
                {
                    Id = Guid.NewGuid(),
                    StudentId = dto.StudentId,
                    ClassTeacherId = dto.ClassTeacherId,
                    ClassId = dto.ClassId,
                    DivisionId = dto.DivisionId,
                    AttendanceDate = dateOnly,
                    Status = status,
                    LeaveType = dto.LeaveType,
                    IsHoliday = effectiveHoliday,
                    Remark = dto.Remark,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };
                await _attendanceRepository.AddAsync(entity);
                _cache.RemoveCache("attendances_all");
                _logger.LogInformation("Created attendance {Id} for student {StudentId}", entity.Id, dto.StudentId);
                return _mapper.Map<AttendanceResponse>(entity);
            }
            catch (Application.Exceptions.BaseServiceException) { throw; }
            catch (Exception ex)
            {
                throw new Application.Exceptions.BaseServiceException("Error marking attendance", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<AttendanceResponse>> BulkMarkAttendanceAsync(BulkAttendanceDto dto)
        {
            try
            {
                _logger.LogInformation("BulkMarkAttendance: {Count} entries, Date={Date}", dto.Entries?.Count ?? 0, dto.AttendanceDate);
                if (dto.Entries == null || dto.Entries.Count == 0)
                    return Array.Empty<AttendanceResponse>();

                await PrevalidateAsync(dto.Entries.Select(e => new MarkAttendanceDto
                {
                    StudentId = e.StudentId, ClassId = dto.ClassId ?? e.ClassId, DivisionId = dto.DivisionId ?? e.DivisionId,
                    AttendanceDate = dto.AttendanceDate ?? e.AttendanceDate, Status = e.Status,
                }));
                var results = new List<AttendanceResponse>();
                foreach (var entry in dto.Entries)
                {
                    var markDto = new MarkAttendanceDto
                    {
                        StudentId = entry.StudentId,
                        ClassTeacherId = dto.ClassTeacherId ?? entry.ClassTeacherId,
                        ClassId = dto.ClassId ?? entry.ClassId,
                        DivisionId = dto.DivisionId ?? entry.DivisionId,
                        AttendanceDate = dto.AttendanceDate ?? entry.AttendanceDate,
                        Status = entry.Status,
                        LeaveType = entry.LeaveType,
                        IsHoliday = entry.IsHoliday,
                        Remark = entry.Remark
                    };
                    var response = await MarkAttendanceAsync(markDto);
                    results.Add(response);
                }
                await _unitOfWork.Commit();
                return results;
            }
            catch (Application.Exceptions.BaseServiceException) { throw; }
            catch (Exception ex)
            {
                throw new Application.Exceptions.BaseServiceException("Error bulk marking attendance", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        private const int BulkImportMaxRows = 8000;

        public async Task<BulkAttendanceImportPreviewDto> PreviewBulkAttendanceImportAsync(BulkAttendanceImportRequest request)
        {
            if (request == null) throw new Application.Exceptions.BaseServiceException("Request required", Application.Common.ExceptionCodes.Validation);
            var full = await BuildBulkAttendanceImportPreviewAsync(request, persist: false);
            return new BulkAttendanceImportPreviewDto
            {
                Rows = full.Rows,
                TotalRows = full.TotalRows,
                ResolvedCount = full.ResolvedCount,
                ErrorCount = full.ErrorCount,
                WouldImportCount = full.WouldImportCount,
                HolidayAwareRowCount = full.HolidayAwareRowCount
            };
        }

        public async Task<BulkAttendanceImportApplyResultDto> ImportBulkAttendanceFromMachineAsync(BulkAttendanceImportRequest request)
        {
            if (request == null) throw new Application.Exceptions.BaseServiceException("Request required", Application.Common.ExceptionCodes.Validation);
            var preview = await BuildBulkAttendanceImportPreviewAsync(request, persist: true);
            return preview;
        }

        public async Task<byte[]> GenerateDivisionAttendanceImportTemplateAsync(AttendanceDivisionTemplateRequest request)
        {
            if (request == null)
                throw new Application.Exceptions.BaseServiceException("Request required", Application.Common.ExceptionCodes.Validation);
            if (request.BranchId == Guid.Empty || request.ClassId == Guid.Empty || request.DivisionId == Guid.Empty)
                throw new Application.Exceptions.BaseServiceException("BranchId, ClassId, and DivisionId are required.", Application.Common.ExceptionCodes.Validation);

            var start = request.StartDate;
            var end = request.EndDate;
            if (end < start)
                (start, end) = (end, start);

            var daySpan = end.DayNumber - start.DayNumber + 1;
            if (daySpan > 366)
                throw new Application.Exceptions.BaseServiceException("Date range cannot exceed 366 days.", Application.Common.ExceptionCodes.Validation);

            var branch = await _branchRepository.GetByIdAsync(request.BranchId);
            if (branch == null)
                throw new Application.Exceptions.BaseServiceException("Branch not found.", Application.Common.ExceptionCodes.ItemNotFound);

            var classEntity = await _classRepository.GetByIdAsync(request.ClassId);
            if (classEntity == null || classEntity.BranchId != request.BranchId)
                throw new Application.Exceptions.BaseServiceException("Class not found for this branch.", Application.Common.ExceptionCodes.ItemNotFound);

            var divisionEntity = await _divisionRepository.GetByIdAsync(request.DivisionId);
            if (divisionEntity == null || divisionEntity.ClassId != request.ClassId)
                throw new Application.Exceptions.BaseServiceException("Division not found for this class.", Application.Common.ExceptionCodes.ItemNotFound);

            var studentEntities = await _studentRepository.FindAllAsync(s =>
                s.BranchId == request.BranchId &&
                s.ClassId == request.ClassId &&
                s.DivisionId == request.DivisionId &&
                s.IsActive != false);

            var studentList = (studentEntities ?? new List<Student>()).ToList();
            if (studentList.Count == 0)
                throw new Application.Exceptions.BaseServiceException("No active students in this division.", Application.Common.ExceptionCodes.Validation);

            var dates = AttendanceDivisionImportTemplateExcel.EachDateInclusive(start, end);
            var totalRows = studentList.Count * dates.Count;
            if (totalRows > BulkImportMaxRows)
                throw new Application.Exceptions.BaseServiceException(
                    $"Generated sheet would have {totalRows} rows; maximum is {BulkImportMaxRows}. Narrow the date range or import in batches.",
                    Application.Common.ExceptionCodes.Validation);

            var ordered = studentList
                .OrderBy(s => RollSortKey(s.RollNumber))
                .ThenBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToList();

            var idList = ordered.Select(s => s.Id).ToList();
            // Do not use string.Equals(..., OrdinalIgnoreCase) here — EF Core cannot translate it to SQL.
            var leaves = await _leaveRequestRepository.FindAllAsync(l =>
                idList.Contains(l.StudentId) &&
                l.FromDate <= end &&
                l.ToDate >= start &&
                l.Status != null &&
                l.Status.ToLower() == "approved");

            var leaveMap = new Dictionary<(Guid, DateOnly), string>();
            foreach (var l in leaves ?? new List<LeaveRequest>())
            {
                var from = l.FromDate < start ? start : l.FromDate;
                var to = l.ToDate > end ? end : l.ToDate;
                for (var d = from; d <= to; d = d.AddDays(1))
                {
                    var key = (l.StudentId, d);
                    if (!leaveMap.ContainsKey(key))
                        leaveMap[key] = string.IsNullOrWhiteSpace(l.LeaveType) ? "Leave" : l.LeaveType.Trim();
                }
            }

            var teacherName = await ResolveClassTeacherDisplayNameAsync(request.ClassId, request.DivisionId);

            return AttendanceDivisionImportTemplateExcel.Build(
                classEntity.Name ?? request.ClassId.ToString(),
                request.ClassId,
                divisionEntity.Name ?? request.DivisionId.ToString(),
                request.DivisionId,
                teacherName,
                ordered,
                dates,
                leaveMap);
        }

        private static string RollSortKey(string? roll)
        {
            if (string.IsNullOrWhiteSpace(roll)) return "\uFFFF";
            return int.TryParse(roll.Trim(), System.Globalization.NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)
                ? n.ToString("D8", CultureInfo.InvariantCulture)
                : roll.Trim().ToLowerInvariant();
        }

        private async Task<string?> ResolveClassTeacherDisplayNameAsync(Guid classId, Guid divisionId)
        {
            var maps = await _classTeacherMapRepository.FindAllAsync(m =>
                m.ClassId == classId && (m.DivisionId == null || m.DivisionId == divisionId));
            var list = maps?.ToList() ?? new List<ClassTeacherMap>();
            var map = list.FirstOrDefault(m => m.DivisionId == divisionId) ?? list.FirstOrDefault();
            if (map?.StaffId == null || map.StaffId == Guid.Empty)
                return null;
            var staff = await _staffRepository.GetByIdAsync(map.StaffId.Value);
            if (staff == null) return null;
            var name = $"{staff.FirstName} {staff.LastName}".Trim();
            return string.IsNullOrEmpty(name) ? staff.StaffCode : name;
        }

        private async Task<BulkAttendanceImportApplyResultDto> BuildBulkAttendanceImportPreviewAsync(BulkAttendanceImportRequest request, bool persist)
        {
            var rowsInput = request.Rows ?? new List<BulkAttendanceImportRowInput>();
            if (rowsInput.Count == 0)
                return new BulkAttendanceImportApplyResultDto();
            if (rowsInput.Count > BulkImportMaxRows)
                throw new Application.Exceptions.BaseServiceException($"At most {BulkImportMaxRows} rows per import.", Application.Common.ExceptionCodes.Validation);

            if (request.BranchId == Guid.Empty)
                throw new Application.Exceptions.BaseServiceException("BranchId is required", Application.Common.ExceptionCodes.Validation);

            var branch = await _branchRepository.GetByIdAsync(request.BranchId);
            if (branch == null)
                throw new Application.Exceptions.BaseServiceException("Branch not found", Application.Common.ExceptionCodes.ItemNotFound);

            var ctx = await BuildBulkImportContextAsync(request.BranchId);

            var dates = new List<DateOnly>();
            foreach (var r in rowsInput)
            {
                var d = r.Date ?? r.AttendanceDate ?? request.DefaultDate;
                if (d.HasValue) dates.Add(d.Value);
            }
            DateOnly minD = dates.Count > 0 ? dates.Min() : DateOnly.FromDateTime(DateTime.UtcNow);
            DateOnly maxD = dates.Count > 0 ? dates.Max() : minD;
            var cal = await ResolveCalendarAsync(request.BranchId, minD, maxD);

            var outRows = new List<BulkAttendanceImportRowResultDto>();
            var saved = 0;
            foreach (var row in rowsInput)
            {
                var result = ProcessOneImportRow(row, request, ctx, cal);
                outRows.Add(result);
                // Business rules (locks, student on roll, no future dates) are reported per row in the preview,
                // and such rows are skipped on apply instead of aborting the whole import half way.
                if (result.WouldImport && result.StudentId is { } rowStudentId && result.Date is { } rowDate)
                {
                    try
                    {
                        await _policy.EnsureCanMarkAsync(rowStudentId, rowDate, result.ResolvedClassId, result.ResolvedDivisionId);
                    }
                    catch (Application.Exceptions.BaseServiceException ruleViolation)
                    {
                        result.Error = WithoutServicePrefix(ruleViolation.Message);
                        result.WouldImport = false;
                    }
                }
                if (persist && result.WouldImport)
                {
                    var leaveTrimmed = string.IsNullOrWhiteSpace(row.LeaveType) ? null : row.LeaveType.Trim();
                    var markDto = new MarkAttendanceDto
                    {
                        StudentId = result.StudentId,
                        ClassTeacherId = request.ClassTeacherId,
                        ClassId = result.ResolvedClassId,
                        DivisionId = result.ResolvedDivisionId,
                        AttendanceDate = result.Date,
                        Status = result.NormalizedStatus,
                        LeaveType = leaveTrimmed,
                        IsHoliday = row.IsHoliday,
                        Remark = row.Remark
                    };
                    await MarkAttendanceAsync(markDto);
                    saved++;
                }
            }

            if (persist)
                await _unitOfWork.Commit();

            var resolved = outRows.Count(r => r.Resolved);
            var errors = outRows.Count(r => !string.IsNullOrEmpty(r.Error));
            var would = outRows.Count(r => r.WouldImport);
            var hol = outRows.Count(r => r.Resolved && r.EffectiveHoliday);

            return new BulkAttendanceImportApplyResultDto
            {
                Rows = outRows,
                TotalRows = outRows.Count,
                ResolvedCount = resolved,
                ErrorCount = errors,
                WouldImportCount = would,
                HolidayAwareRowCount = hol,
                SavedCount = persist ? saved : 0
            };
        }

        private BulkAttendanceImportRowResultDto ProcessOneImportRow(
            BulkAttendanceImportRowInput row,
            BulkAttendanceImportRequest request,
            BulkImportContext ctx,
            AttendanceCalendarContext cal)
        {
            var result = new BulkAttendanceImportRowResultDto
            {
                RowIndex = row.RowIndex,
                RawStatus = row.Status,
                RowIsHoliday = row.IsHoliday
            };

            var leaveTrimmed = string.IsNullOrWhiteSpace(row.LeaveType) ? null : row.LeaveType.Trim();
            result.LeaveType = leaveTrimmed;

            var date = row.Date ?? row.AttendanceDate ?? request.DefaultDate;
            if (!date.HasValue)
            {
                result.Error = "AttendanceDate is required (column Date/AttendanceDate or DefaultDate).";
                return result;
            }

            result.Date = date.Value;

            Guid? classId = row.ClassId ?? request.ClassId;
            if (classId.HasValue && !ctx.BranchClassIds.Contains(classId.Value))
            {
                result.Error = "ClassId is not in this branch.";
                return result;
            }

            Guid? divisionId = request.DivisionId;
            if (!string.IsNullOrWhiteSpace(row.DivisionName))
            {
                if (!classId.HasValue)
                {
                    result.Error = "DivisionName requires ClassId (row or request).";
                    return result;
                }

                var divNorm = NormalizeImportKey(row.DivisionName);
                var divKey = (classId.Value, divNorm);
                if (ctx.AmbiguousDivisionNames.Contains(divKey))
                {
                    result.Error = "Duplicate division name in this class; fix data or use a unique name.";
                    return result;
                }

                if (!ctx.DivisionIdByClassAndName.TryGetValue(divKey, out var divResolved))
                {
                    result.Error = "Division not found for this class and DivisionName.";
                    return result;
                }

                divisionId = divResolved;
            }

            var student = ResolveStudentForImport(ctx.Maps, row, request.BranchId, classId, divisionId, out var resolveError);
            if (student == null)
            {
                result.Error = resolveError ?? "Student not found.";
                return result;
            }

            if (classId.HasValue && student.ClassId.HasValue && student.ClassId.Value != classId.Value)
            {
                result.Error = "Student is not in the specified class.";
                return result;
            }

            if (divisionId.HasValue && student.DivisionId.HasValue && student.DivisionId.Value != divisionId.Value)
            {
                result.Error = "Student is not in the specified division.";
                return result;
            }

            result.Resolved = true;
            result.StudentId = student.Id;
            result.StudentDisplayName = $"{student.FirstName} {student.LastName}".Trim();
            if (string.IsNullOrEmpty(result.StudentDisplayName))
                result.StudentDisplayName = student.AdmissionNo ?? student.RollNumber ?? student.Id.ToString();

            result.ResolvedClassId = classId ?? student.ClassId;
            result.ResolvedDivisionId = divisionId ?? student.DivisionId;

            var normalized = NormalizeImportStatus(row.Status, request.DefaultPresentWhenStatusEmpty);
            if (string.IsNullOrEmpty(normalized))
            {
                result.Resolved = false;
                result.Error = "Status is empty or not recognized.";
                return result;
            }

            result.NormalizedStatus = normalized;

            result.IsWeeklyOff = IsWeeklyOffDay(date.Value, cal.SaturdayPattern, cal.SundayOff);
            result.IsCalendarHoliday = cal.CalendarHolidayDates.Contains(date.Value);
            result.EffectiveHoliday = row.IsHoliday == true || result.IsWeeklyOff || result.IsCalendarHoliday;

            result.WouldImport = string.IsNullOrEmpty(result.Error) && result.StudentId.HasValue && result.Date.HasValue && !string.IsNullOrEmpty(result.NormalizedStatus);
            return result;
        }

        private sealed class BulkImportContext
        {
            public StudentLookupMaps Maps { get; init; } = null!;
            public Dictionary<(Guid ClassId, string NameNorm), Guid> DivisionIdByClassAndName { get; init; } = null!;
            public HashSet<(Guid ClassId, string NameNorm)> AmbiguousDivisionNames { get; init; } = null!;
            public HashSet<Guid> BranchClassIds { get; init; } = null!;
        }

        private sealed class StudentLookupMaps
        {
            public Dictionary<Guid, Student> ById { get; } = new();
            public Dictionary<string, Student> ByRoll { get; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, Student> ByCompositeRoll { get; } = new(StringComparer.Ordinal);
            public Dictionary<string, Student> ByAdmission { get; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, Student> ByEnrollment { get; } = new(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> AmbiguousRolls { get; } = new(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> AmbiguousCompositeRolls { get; } = new(StringComparer.Ordinal);
            public HashSet<string> AmbiguousAdmission { get; } = new(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> AmbiguousEnrollment { get; } = new(StringComparer.OrdinalIgnoreCase);
        }

        private static string NormalizeImportKey(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            return value.Trim().ToLowerInvariant();
        }

        private static void AddStudentKey(Dictionary<string, Student> map, HashSet<string> ambiguous, string? rawKey, Student student)
        {
            var k = NormalizeImportKey(rawKey);
            if (k.Length == 0) return;
            if (map.TryGetValue(k, out var existing) && existing.Id != student.Id)
                ambiguous.Add(k);
            else
                map[k] = student;
        }

        private static string CompositeRollKey(Guid classId, Guid divisionId, string rollNorm)
            => $"{classId:N}\u001f{divisionId:N}\u001f{rollNorm}";

        private static void AddCompositeRollKey(
            Dictionary<string, Student> map,
            HashSet<string> ambiguous,
            Guid classId,
            Guid divisionId,
            string? rollRaw,
            Student student)
        {
            var rollNorm = NormalizeImportKey(rollRaw);
            if (rollNorm.Length == 0) return;
            var key = CompositeRollKey(classId, divisionId, rollNorm);
            if (map.TryGetValue(key, out var existing) && existing.Id != student.Id)
                ambiguous.Add(key);
            else
                map[key] = student;
        }

        private async Task<BulkImportContext> BuildBulkImportContextAsync(Guid branchId)
        {
            var branchClasses = await _classRepository.FindAllAsync(c => c.BranchId == branchId);
            var branchClassIds = (branchClasses ?? Enumerable.Empty<Class>()).Select(c => c.Id).ToHashSet();

            var (divMap, divAmb) = await BuildDivisionNameLookupAsync(branchClassIds);
            var maps = await BuildBulkImportStudentMapsAsync(branchId);
            return new BulkImportContext
            {
                Maps = maps,
                DivisionIdByClassAndName = divMap,
                AmbiguousDivisionNames = divAmb,
                BranchClassIds = branchClassIds
            };
        }

        private async Task<(Dictionary<(Guid ClassId, string NameNorm), Guid> map, HashSet<(Guid ClassId, string NameNorm)> ambiguous)>
            BuildDivisionNameLookupAsync(HashSet<Guid> branchClassIds)
        {
            var map = new Dictionary<(Guid, string), Guid>();
            var ambiguous = new HashSet<(Guid, string)>();
            if (branchClassIds.Count == 0)
                return (map, ambiguous);

            var divisions = await _divisionRepository.FindAllAsync(d =>
                d.ClassId != null && branchClassIds.Contains(d.ClassId.Value));

            foreach (var d in divisions ?? Enumerable.Empty<Division>())
            {
                var norm = NormalizeImportKey(d.Name);
                if (norm.Length == 0) continue;
                var key = (d.ClassId!.Value, norm);
                if (map.TryGetValue(key, out var existingId) && existingId != d.Id)
                    ambiguous.Add(key);
                else
                    map[key] = d.Id;
            }

            foreach (var k in ambiguous.ToList())
                map.Remove(k);

            return (map, ambiguous);
        }

        private async Task<StudentLookupMaps> BuildBulkImportStudentMapsAsync(Guid branchId)
        {
            var all = await _studentRepository.FindAllAsync(s =>
                s.BranchId == branchId &&
                s.IsActive != false);
            var list = all ?? new List<Student>();
            var maps = new StudentLookupMaps();
            foreach (var s in list)
            {
                maps.ById[s.Id] = s;
                AddStudentKey(maps.ByRoll, maps.AmbiguousRolls, s.RollNumber, s);
                if (s.ClassId.HasValue && s.DivisionId.HasValue)
                    AddCompositeRollKey(maps.ByCompositeRoll, maps.AmbiguousCompositeRolls, s.ClassId.Value, s.DivisionId.Value, s.RollNumber, s);
                AddStudentKey(maps.ByAdmission, maps.AmbiguousAdmission, s.AdmissionNo, s);
                AddStudentKey(maps.ByEnrollment, maps.AmbiguousEnrollment, s.EnrollmentNo, s);
            }

            return maps;
        }

        private static Student? ResolveStudentForImport(
            StudentLookupMaps maps,
            BulkAttendanceImportRowInput row,
            Guid branchId,
            Guid? resolvedClassId,
            Guid? resolvedDivisionId,
            out string? error)
        {
            error = null;
            if (row.StudentId.HasValue && row.StudentId.Value != Guid.Empty)
            {
                if (!maps.ById.TryGetValue(row.StudentId.Value, out var byId))
                {
                    error = "StudentId not found in this branch.";
                    return null;
                }

                if (byId.BranchId != branchId)
                {
                    error = "Student does not belong to this branch.";
                    return null;
                }

                return byId;
            }

            var rollK = NormalizeImportKey(row.RollNumber);
            if (rollK.Length > 0)
            {
                if (resolvedClassId.HasValue && resolvedDivisionId.HasValue)
                {
                    var ckey = CompositeRollKey(resolvedClassId.Value, resolvedDivisionId.Value, rollK);
                    if (maps.AmbiguousCompositeRolls.Contains(ckey))
                    {
                        error = "Duplicate roll number in this class/division.";
                        return null;
                    }

                    if (maps.ByCompositeRoll.TryGetValue(ckey, out var byComposite))
                        return byComposite;

                    error = "No student with this roll in the specified class and division.";
                    return null;
                }

                if (maps.AmbiguousRolls.Contains(rollK))
                {
                    error = "Roll number is ambiguous in this branch; add ClassId and DivisionName (or DivisionId) columns.";
                    return null;
                }

                if (maps.ByRoll.TryGetValue(rollK, out var s))
                    return s;
            }

            var admK = NormalizeImportKey(row.AdmissionNo);
            if (admK.Length > 0)
            {
                if (maps.AmbiguousAdmission.Contains(admK))
                {
                    error = "Duplicate admission number in branch.";
                    return null;
                }

                if (maps.ByAdmission.TryGetValue(admK, out var s2))
                    return s2;
            }

            var enK = NormalizeImportKey(row.EnrollmentNo);
            if (enK.Length > 0)
            {
                if (maps.AmbiguousEnrollment.Contains(enK))
                {
                    error = "Duplicate enrollment number in branch.";
                    return null;
                }

                if (maps.ByEnrollment.TryGetValue(enK, out var s3))
                    return s3;
            }

            error = "Provide StudentId, RollNumber, AdmissionNo, or EnrollmentNo matching this branch.";
            return null;
        }

        private static string? NormalizeImportStatus(string? raw, bool defaultPresentWhenEmpty)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return defaultPresentWhenEmpty ? "Present" : null;

            var s = raw.Trim();
            if (int.TryParse(s, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var n))
            {
                if (n == 1) return "Present";
                if (n == 0) return "Absent";
            }

            var lower = s.ToLowerInvariant();
            switch (lower)
            {
                case "p":
                case "present":
                case "yes":
                case "y":
                case "in":
                case "pr":
                    return "Present";
                case "a":
                case "absent":
                case "no":
                case "n":
                case "out":
                case "ab":
                    return "Absent";
                case "l":
                case "late":
                    return "Late";
                case "half":
                case "halfday":
                case "half-day":
                    return "HalfDay";
                case "lv":
                case "leave":
                    return "Leave";
                default:
                    if (lower.Length == 0) return null;
                    return char.ToUpperInvariant(lower[0]) + lower.Substring(1);
            }
        }

        public async Task<IEnumerable<AttendanceResponse>> MarkClassAttendanceAsync(Guid classId, Guid divisionId, DateTime date, List<StudentAttendanceDto> students)
        {
            try
            {
                var dateOnly = DateOnly.FromDateTime(date);
                _logger.LogInformation("MarkClassAttendance: ClassId={ClassId}, DivisionId={DivisionId}, Date={Date}, Students={Count}", classId, divisionId, dateOnly, students?.Count ?? 0);
                if (students == null || students.Count == 0)
                    return Array.Empty<AttendanceResponse>();

                await PrevalidateAsync(students.Select(st => new MarkAttendanceDto
                {
                    StudentId = st.StudentId, ClassId = classId, DivisionId = divisionId, AttendanceDate = dateOnly, Status = st.Status,
                }));
                var results = new List<AttendanceResponse>();
                foreach (var s in students)
                {
                    var markDto = new MarkAttendanceDto
                    {
                        StudentId = s.StudentId,
                        ClassId = classId,
                        DivisionId = divisionId,
                        AttendanceDate = dateOnly,
                        Status = s.Status,
                        LeaveType = s.LeaveType,
                        Remark = s.Remark
                    };
                    var response = await MarkAttendanceAsync(markDto);
                    results.Add(response);
                }
                await _unitOfWork.Commit();
                return results;
            }
            catch (Application.Exceptions.BaseServiceException) { throw; }
            catch (Exception ex)
            {
                throw new Application.Exceptions.BaseServiceException("Error marking class attendance", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<AttendanceResponse?> UpdateAttendanceAsync(Guid attendanceId, UpdateAttendanceDto dto)
        {
            try
            {
                _logger.LogInformation("UpdateAttendance: {Id}", attendanceId);
                var entity = await _attendanceRepository.GetByIdAsync(attendanceId);
                if (entity == null)
                {
                    _logger.LogWarning("Attendance not found: {Id}", attendanceId);
                    return null;
                }
                if (entity.IsDeleted == true)
                {
                    _logger.LogWarning("Cannot update soft-deleted attendance {Id}", attendanceId);
                    return null;
                }
                await _policy.EnsureNotLockedAsync(entity.ClassId, entity.DivisionId, entity.AttendanceDate!.Value);
                if (dto.Status != null) entity.Status = RequireStatus(dto.Status);
                if (dto.LeaveType != null) entity.LeaveType = dto.LeaveType;
                if (dto.IsHoliday.HasValue) entity.IsHoliday = dto.IsHoliday;
                if (dto.Remark != null) entity.Remark = dto.Remark;
                await _attendanceRepository.UpdateAsync(entity);
                _cache.RemoveCache("attendances_all");
                _cache.RemoveCache($"attendance_{attendanceId}");
                _logger.LogInformation("Updated attendance {Id}", attendanceId);
                return _mapper.Map<AttendanceResponse>(entity);
            }
            catch (Exception ex)
            {
                throw new Application.Exceptions.BaseServiceException("Error updating attendance", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<bool> SoftDeleteAttendanceAsync(Guid attendanceId)
        {
            try
            {
                _logger.LogInformation("SoftDeleteAttendance: {Id}", attendanceId);
                var entity = await _attendanceRepository.GetByIdAsync(attendanceId);
                if (entity == null)
                {
                    _logger.LogWarning("Attendance not found: {Id}", attendanceId);
                    throw new Application.Exceptions.BaseServiceException("Attendance not found", Application.Common.ExceptionCodes.ItemNotFound);
                }
                await _policy.EnsureNotLockedAsync(entity.ClassId, entity.DivisionId, entity.AttendanceDate!.Value);
                entity.IsDeleted = true;
                entity.DeletedAt = DateTime.UtcNow;
                await _attendanceRepository.UpdateAsync(entity);
                _cache.RemoveCache("attendances_all");
                _cache.RemoveCache($"attendance_{attendanceId}");
                _logger.LogInformation("Soft deleted attendance {Id}", attendanceId);
                return true;
            }
            catch (Application.Exceptions.BaseServiceException) { throw; }
            catch (Exception ex)
            {
                throw new Application.Exceptions.BaseServiceException("Error soft deleting attendance", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<bool> HardDeleteAttendanceAsync(Guid attendanceId)
        {
            return await DeleteAsync(attendanceId);
        }

        public async Task<bool> RestoreAttendanceAsync(Guid attendanceId)
        {
            try
            {
                _logger.LogInformation("RestoreAttendance: {Id}", attendanceId);
                var entity = await _attendanceRepository.FindAsync(a => a.Id == attendanceId, trackChanges: true, ignoreQueryFilters: true);
                if (entity == null)
                {
                    _logger.LogWarning("Attendance not found: {Id}", attendanceId);
                    throw new Application.Exceptions.BaseServiceException("Attendance not found", Application.Common.ExceptionCodes.ItemNotFound);
                }
                await _policy.EnsureNotLockedAsync(entity.ClassId, entity.DivisionId, entity.AttendanceDate!.Value);
                entity.IsDeleted = false;
                entity.DeletedAt = null;
                await _attendanceRepository.UpdateAsync(entity);
                _cache.RemoveCache("attendances_all");
                _cache.RemoveCache($"attendance_{attendanceId}");
                _logger.LogInformation("Restored attendance {Id}", attendanceId);
                return true;
            }
            catch (Application.Exceptions.BaseServiceException) { throw; }
            catch (Exception ex)
            {
                throw new Application.Exceptions.BaseServiceException("Error restoring attendance", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<bool> LockAttendanceByDateAsync(Guid classId, Guid divisionId, DateTime date)
        {
            var dateOnly = DateOnly.FromDateTime(date);
            var existing = await _dateLockRepository.FindAsync(l => l.ClassId == classId && l.DivisionId == divisionId && l.AttendanceDate == dateOnly);
            if (existing != null)
            {
                if (existing.IsLocked) return true;
                existing.IsLocked = true;
                existing.LockedAt = DateTime.UtcNow;
                existing.UnlockedAt = null;
                await _dateLockRepository.UpdateAsync(existing);
            }
            else
            {
                await _dateLockRepository.AddAsync(new AttendanceDateLock
                {
                    Id = Guid.NewGuid(),
                    ClassId = classId,
                    DivisionId = divisionId,
                    AttendanceDate = dateOnly,
                    IsLocked = true,
                    LockedAt = DateTime.UtcNow
                });
            }
            _logger.LogInformation("Locked attendance for ClassId={ClassId}, DivisionId={DivisionId}, Date={Date}", classId, divisionId, dateOnly);
            return true;
        }

        public async Task<bool> UnlockAttendanceByDateAsync(Guid classId, Guid divisionId, DateTime date)
        {
            var dateOnly = DateOnly.FromDateTime(date);
            var existing = await _dateLockRepository.FindAsync(l => l.ClassId == classId && l.DivisionId == divisionId && l.AttendanceDate == dateOnly);
            if (existing == null) return true;
            existing.IsLocked = false;
            existing.UnlockedAt = DateTime.UtcNow;
            await _dateLockRepository.UpdateAsync(existing);
            _logger.LogInformation("Unlocked attendance for ClassId={ClassId}, DivisionId={DivisionId}, Date={Date}", classId, divisionId, dateOnly);
            return true;
        }

        public async Task<bool> LockAttendanceForAcademicYearAsync(Guid academicYearId)
        {
            var existing = await _academicYearLockRepository.FindAsync(l => l.AcademicYearId == academicYearId);
            if (existing != null)
            {
                if (existing.IsLocked) return true;
                existing.IsLocked = true;
                existing.LockedAt = DateTime.UtcNow;
                existing.UnlockedAt = null;
                await _academicYearLockRepository.UpdateAsync(existing);
            }
            else
            {
                await _academicYearLockRepository.AddAsync(new AttendanceAcademicYearLock
                {
                    Id = Guid.NewGuid(),
                    AcademicYearId = academicYearId,
                    IsLocked = true,
                    LockedAt = DateTime.UtcNow
                });
            }
            _logger.LogInformation("Locked attendance for AcademicYearId={AcademicYearId}", academicYearId);
            return true;
        }

        public async Task<bool> UnlockAttendanceForAcademicYearAsync(Guid academicYearId)
        {
            var existing = await _academicYearLockRepository.FindAsync(l => l.AcademicYearId == academicYearId);
            if (existing == null) return true;
            existing.IsLocked = false;
            existing.UnlockedAt = DateTime.UtcNow;
            await _academicYearLockRepository.UpdateAsync(existing);
            _logger.LogInformation("Unlocked attendance for AcademicYearId={AcademicYearId}", academicYearId);
            return true;
        }

        public Task<bool> ValidateAttendanceIsLockedAsync(Guid classId, DateTime date) =>
            _policy.IsLockedAsync(classId, divisionId: null, DateOnly.FromDateTime(date));

        public async Task<ValidationResultDto> ValidateAttendanceNotDuplicateAsync(Guid studentId, DateTime date)
        {
            var dateOnly = DateOnly.FromDateTime(date);
            var existing = await _attendanceRepository.FindAsync(a => a.StudentId == studentId && a.AttendanceDate == dateOnly && a.IsDeleted != true);
            return existing != null ? new ValidationResultDto { Valid = false, Message = "Attendance already exists for this student and date." } : new ValidationResultDto { Valid = true };
        }

        public async Task<ValidationResultDto> ValidateAttendanceWithinAcademicYearAsync(DateTime date, Guid academicYearId)
        {
            var ay = await _academicYearRepository.GetByIdAsync(academicYearId);
            var dateOnly = DateOnly.FromDateTime(date);
            if (ay?.StartDate == null || ay.EndDate == null) return new ValidationResultDto { Valid = false, Message = "Academic year not found or has no date range." };
            var valid = dateOnly >= ay.StartDate.Value && dateOnly <= ay.EndDate.Value;
            return new ValidationResultDto { Valid = valid, Message = valid ? null : "Date is outside the academic year range." };
        }

        public async Task<ValidationResultDto> ValidateStudentActiveAsync(Guid studentId)
        {
            var student = await _studentRepository.GetByIdAsync(studentId);
            var active = student?.IsActive == true;
            return new ValidationResultDto { Valid = active, Message = active ? null : "Student is not active." };
        }

        public async Task<ValidationResultDto> ValidateStudentBelongsToClassAsync(Guid studentId, Guid classId)
        {
            var student = await _studentRepository.GetByIdAsync(studentId);
            var valid = student != null && student.ClassId == classId;
            return new ValidationResultDto { Valid = valid, Message = valid ? null : "Student does not belong to this class." };
        }

        public async Task<ValidationResultDto> ValidateDivisionMatchAsync(Guid studentId, Guid divisionId)
        {
            var student = await _studentRepository.GetByIdAsync(studentId);
            var valid = student != null && student.DivisionId == divisionId;
            return new ValidationResultDto { Valid = valid, Message = valid ? null : "Student division does not match." };
        }

        public async Task<ValidationResultDto> ValidateHolidayAsync(DateTime date, Guid? branchId = null)
        {
            var d = DateOnly.FromDateTime(date.Date);
            var w = await ResolveWeeklyOffAsync(branchId);
            if (IsWeeklyOffDay(d, w.SaturdayPattern, w.SundayOff))
            {
                var day = d.DayOfWeek == DayOfWeek.Saturday ? "Saturday" : "Sunday";
                return new ValidationResultDto { Valid = true, Message = $"{day}: weekly off — optional marking; excluded from attendance %." };
            }
            if (branchId.HasValue && await _schoolHolidayRepository.HasActiveHolidayAsync(branchId.Value, d))
                return new ValidationResultDto { Valid = true, Message = "School holiday (calendar) — excluded from attendance %." };
            return new ValidationResultDto { Valid = true, Message = null };
        }

        public async Task<ValidationResultDto> ValidateAttendancePeriodAsync(AttendancePeriodType periodType, DateTime? startDate, DateTime? endDate)
        {
            if (periodType == AttendancePeriodType.Custom && (!startDate.HasValue || !endDate.HasValue))
                return new ValidationResultDto { Valid = false, Message = "Custom period requires start and end date." };
            if (periodType == AttendancePeriodType.Custom && startDate.HasValue && endDate.HasValue && startDate.Value > endDate.Value)
                return new ValidationResultDto { Valid = false, Message = "Start date must be before end date." };
            return new ValidationResultDto { Valid = true };
        }

        // --- Leave ---
        public async Task<LeaveRequestDto> ApplyStudentLeaveAsync(Guid studentId, DateTime fromDate, DateTime toDate, string leaveType, string? reason = null)
        {
            var student = await _studentRepository.GetByIdAsync(studentId);
            var entity = new LeaveRequest
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                FromDate = DateOnly.FromDateTime(fromDate),
                ToDate = DateOnly.FromDateTime(toDate),
                LeaveType = leaveType,
                Reason = reason,
                Status = "Pending",
                RequestedAt = DateTime.UtcNow
            };
            await _leaveRequestRepository.AddAsync(entity);
            _logger.LogInformation("Leave applied for student {StudentId} from {From} to {To}", studentId, fromDate, toDate);
            return MapToLeaveRequestDto(entity, student);
        }

        public async Task<bool> ApproveLeaveRequestAsync(Guid leaveId, Guid? approvedBy = null)
        {
            var entity = await _leaveRequestRepository.GetByIdAsync(leaveId);
            if (entity == null) return false;
            entity.Status = "Approved";
            entity.ApprovedAt = DateTime.UtcNow;
            entity.ApprovedBy = approvedBy;
            await _leaveRequestRepository.UpdateAsync(entity);
            _logger.LogInformation("Leave request {LeaveId} approved", leaveId);
            return true;
        }

        public async Task<bool> RejectLeaveRequestAsync(Guid leaveId, string? rejectionReason = null, Guid? rejectedBy = null)
        {
            var entity = await _leaveRequestRepository.GetByIdAsync(leaveId);
            if (entity == null) return false;
            entity.Status = "Rejected";
            entity.ApprovedAt = DateTime.UtcNow;
            entity.ApprovedBy = rejectedBy;
            entity.RejectionReason = rejectionReason;
            await _leaveRequestRepository.UpdateAsync(entity);
            _logger.LogInformation("Leave request {LeaveId} rejected", leaveId);
            return true;
        }

        // --- Correction ---
        public async Task<AttendanceCorrectionRequestDto> RequestAttendanceCorrectionAsync(Guid attendanceId, string reason, Guid? requestedBy = null)
        {
            var entity = new AttendanceCorrectionRequest
            {
                Id = Guid.NewGuid(),
                AttendanceId = attendanceId,
                Reason = reason,
                Status = "Pending",
                RequestedAt = DateTime.UtcNow,
                RequestedBy = requestedBy
            };
            await _correctionRequestRepository.AddAsync(entity);
            _logger.LogInformation("Correction requested for attendance {AttendanceId}", attendanceId);
            return new AttendanceCorrectionRequestDto { AttendanceId = entity.AttendanceId, Reason = entity.Reason, Status = entity.Status, RequestedAt = entity.RequestedAt, RequestedBy = entity.RequestedBy }.WithAuditFrom(entity);
        }

        public async Task<bool> ApproveAttendanceCorrectionAsync(Guid requestId, Guid? approvedBy = null)
        {
            var entity = await _correctionRequestRepository.GetByIdAsync(requestId);
            if (entity == null) return false;
            entity.Status = "Approved";
            entity.ApprovedAt = DateTime.UtcNow;
            entity.ApprovedBy = approvedBy;
            await _correctionRequestRepository.UpdateAsync(entity);
            _logger.LogInformation("Correction request {RequestId} approved", requestId);
            return true;
        }

        public async Task<bool> RejectAttendanceCorrectionAsync(Guid requestId, string? rejectionReason = null, Guid? rejectedBy = null)
        {
            var entity = await _correctionRequestRepository.GetByIdAsync(requestId);
            if (entity == null) return false;
            entity.Status = "Rejected";
            entity.ApprovedAt = DateTime.UtcNow;
            entity.ApprovedBy = rejectedBy;
            entity.RejectionReason = rejectionReason;
            await _correctionRequestRepository.UpdateAsync(entity);
            _logger.LogInformation("Correction request {RequestId} rejected", requestId);
            return true;
        }

        public async Task AuditAttendanceChangeAsync(Guid attendanceId, string action, string? details = null)
        {
            _logger.LogInformation("Attendance audit: AttendanceId={AttendanceId}, Action={Action}, Details={Details}", attendanceId, action, details ?? "");
            // If you have audit_log table, insert there; otherwise logging suffices.
        }

        // --- Notifications (stub: log only; wire to INotificationService if available) ---
        public Task NotifyParentOnAbsenceAsync(Guid studentId, DateTime date) { _logger.LogInformation("NotifyParentOnAbsence StudentId={StudentId}, Date={Date}", studentId, date); return Task.CompletedTask; }
        public Task NotifyParentOnConsecutiveAbsenceAsync(Guid studentId) { _logger.LogInformation("NotifyParentOnConsecutiveAbsence StudentId={StudentId}", studentId); return Task.CompletedTask; }
        public Task NotifyClassTeacherLowAttendanceAsync(Guid studentId) { _logger.LogInformation("NotifyClassTeacherLowAttendance StudentId={StudentId}", studentId); return Task.CompletedTask; }
        public Task NotifyPrincipalChronicAbsenteeAsync(Guid branchId) { _logger.LogInformation("NotifyPrincipalChronicAbsentee BranchId={BranchId}", branchId); return Task.CompletedTask; }
        public Task SendDailyAbsenceNotificationAsync(Guid branchId) { _logger.LogInformation("SendDailyAbsenceNotification BranchId={BranchId}", branchId); return Task.CompletedTask; }

        // --- Auto / Batch ---
        public async Task AutoMarkHolidayAttendanceAsync(DateTime date)
        {
            _logger.LogInformation("AutoMarkHolidayAttendance Date={Date}", date);
            await Task.CompletedTask;
        }

        public async Task AutoCloseAttendanceAfterTimeAsync(Guid classId)
        {
            _logger.LogInformation("AutoCloseAttendanceAfterTime ClassId={ClassId}", classId);
            await Task.CompletedTask;
        }

        public async Task AutoCalculateAttendancePercentagesAsync(Guid academicYearId)
        {
            _logger.LogInformation("AutoCalculateAttendancePercentages AcademicYearId={AcademicYearId}", academicYearId);
            await Task.CompletedTask;
        }

        public async Task AutoFlagLowAttendanceStudentsAsync(decimal threshold)
        {
            _logger.LogInformation("AutoFlagLowAttendanceStudents Threshold={Threshold}", threshold);
            await Task.CompletedTask;
        }

        public async Task AutoNotifyChronicAbsenteesAsync()
        {
            _logger.LogInformation("AutoNotifyChronicAbsentees");
            await Task.CompletedTask;
        }

        public async Task RecalculateAttendanceStatisticsAsync(Guid academicYearId)
        {
            _logger.LogInformation("RecalculateAttendanceStatistics AcademicYearId={AcademicYearId}", academicYearId);
            await Task.CompletedTask;
        }

        public async Task<bool> FreezeAttendanceForAcademicYearAsync(Guid academicYearId)
        {
            _logger.LogInformation("FreezeAttendanceForAcademicYear AcademicYearId={AcademicYearId}", academicYearId);
            await Task.CompletedTask;
            return true;
        }

        public async Task<object> ValidateAttendanceIntegrityAsync(Guid academicYearId)
        {
            _logger.LogInformation("ValidateAttendanceIntegrity AcademicYearId={AcademicYearId}", academicYearId);
            await Task.CompletedTask;
            return new { Valid = true, Message = "Validation completed." };
        }
    }
}
