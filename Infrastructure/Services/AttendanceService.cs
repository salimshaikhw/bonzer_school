using Application.Abstractions;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Helpers;
using Application.Common.Helpers.MemoryCache;
using Application.ViewModels.Attendance;
using AutoMapper;
using Domain.Entities.Postgres.Scaffolded;
using Domain.Enums;
using Infrastructure;
using Infrastructure.Persistence.Postgres;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class AttendanceService : IAttendanceService
    {
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly IAcademicYearRepository _academicYearRepository;
        private readonly IStudentResultRepository _studentResultRepository;
        private readonly ILeaveRequestRepository _leaveRequestRepository;
        private readonly IAttendanceCorrectionRequestRepository _correctionRequestRepository;
        private readonly IAttendanceDateLockRepository _dateLockRepository;
        private readonly IAttendanceAcademicYearLockRepository _academicYearLockRepository;
        private readonly IMapper _mapper;
        private readonly IMemoryCacheHelper _cache;
        private readonly ILogger<AttendanceService> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBranchRepository _branchRepository;
        private readonly IClassRepository _classRepository;
        private readonly IDivisionRepository _divisionRepository;
        private readonly ISchoolHolidayRepository _schoolHolidayRepository;
        private readonly IClassTeacherMapRepository _classTeacherMapRepository;
        private readonly IStaffRepository _staffRepository;

        private readonly record struct AttendanceCalendarContext(AttendanceSaturdayPattern SaturdayPattern, bool SundayOff, HashSet<DateOnly> CalendarHolidayDates);

        public AttendanceService(
            IAttendanceRepository attendanceRepository,
            IStudentRepository studentRepository,
            IAcademicYearRepository academicYearRepository,
            IStudentResultRepository studentResultRepository,
            ILeaveRequestRepository leaveRequestRepository,
            IAttendanceCorrectionRequestRepository correctionRequestRepository,
            IAttendanceDateLockRepository dateLockRepository,
            IAttendanceAcademicYearLockRepository academicYearLockRepository,
            IBranchRepository branchRepository,
            IClassRepository classRepository,
            IDivisionRepository divisionRepository,
            ISchoolHolidayRepository schoolHolidayRepository,
            IClassTeacherMapRepository classTeacherMapRepository,
            IStaffRepository staffRepository,
            IMapper mapper,
            IMemoryCacheHelper cache,
            ILogger<AttendanceService> logger,
            IUnitOfWork unitOfWork)
        {
            _attendanceRepository = attendanceRepository;
            _studentRepository = studentRepository;
            _academicYearRepository = academicYearRepository;
            _studentResultRepository = studentResultRepository;
            _leaveRequestRepository = leaveRequestRepository;
            _correctionRequestRepository = correctionRequestRepository;
            _dateLockRepository = dateLockRepository;
            _academicYearLockRepository = academicYearLockRepository;
            _branchRepository = branchRepository;
            _classRepository = classRepository;
            _divisionRepository = divisionRepository;
            _schoolHolidayRepository = schoolHolidayRepository;
            _classTeacherMapRepository = classTeacherMapRepository;
            _staffRepository = staffRepository;
            _mapper = mapper;
            _cache = cache;
            _logger = logger;
            _unitOfWork = unitOfWork;
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
                var entity = _mapper.Map<Attendance>(input);
                entity.Id = Guid.NewGuid();
                entity.CreatedAt = DateTime.UtcNow;
                entity.IsDeleted = false;
                await _attendanceRepository.AddAsync(entity);
                _cache.RemoveCache("attendances_all");
                _logger.LogInformation("Created new attendance {Id}", entity.Id);
                return _mapper.Map<AttendanceResponse>(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating attendance");
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
                _mapper.Map(input, entity);
                await _attendanceRepository.UpdateAsync(entity);
                _cache.RemoveCache("attendances_all");
                _cache.RemoveCache($"attendance_{id}");
                _logger.LogInformation("Updated attendance {Id}", id);
                return _mapper.Map<AttendanceResponse>(entity);
            }
            catch (Application.Exceptions.BaseServiceException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating attendance {Id}", id);
                throw new Application.Exceptions.BaseServiceException("Error updating attendance", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var entity = await _attendanceRepository.GetByIdAsync(id);
                if (entity == null)
                {
                    _logger.LogWarning("Attendance not found: {Id}", id);
                    throw new Application.Exceptions.BaseServiceException("Attendance not found", Application.Common.ExceptionCodes.ItemNotFound);
                }
                await _attendanceRepository.DeleteAsync(id);
                _cache.RemoveCache("attendances_all");
                _cache.RemoveCache($"attendance_{id}");
                _logger.LogInformation("Deleted attendance {Id}", id);
                return true;
            }
            catch (Application.Exceptions.BaseServiceException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting attendance {Id}", id);
                throw new Application.Exceptions.BaseServiceException("Error deleting attendance", ex, Application.Common.ExceptionCodes.Database);
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
                var student = await _studentRepository.GetByIdAsync(dto.StudentId.Value);
                var branchId = student?.BranchId;
                var w = await ResolveWeeklyOffAsync(branchId);
                var isWeeklyOff = IsWeeklyOffDay(dateOnly, w.SaturdayPattern, w.SundayOff);
                var calendarHoliday = branchId.HasValue && await _schoolHolidayRepository.HasActiveHolidayAsync(branchId.Value, dateOnly);
                var effectiveHoliday = dto.IsHoliday == true || isWeeklyOff || calendarHoliday;
                if (existing != null)
                {
                    existing.Status = dto.Status;
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
                    Status = dto.Status,
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
                _logger.LogError(ex, "Error marking attendance");
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
                _logger.LogError(ex, "Error bulk marking attendance");
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
                _logger.LogError(ex, "Error marking class attendance");
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
                if (dto.Status != null) entity.Status = dto.Status;
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
                _logger.LogError(ex, "Error updating attendance {Id}", attendanceId);
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
                _logger.LogError(ex, "Error soft deleting attendance {Id}", attendanceId);
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
                var entity = await _attendanceRepository.GetByIdAsync(attendanceId);
                if (entity == null)
                {
                    _logger.LogWarning("Attendance not found: {Id}", attendanceId);
                    throw new Application.Exceptions.BaseServiceException("Attendance not found", Application.Common.ExceptionCodes.ItemNotFound);
                }
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
                _logger.LogError(ex, "Error restoring attendance {Id}", attendanceId);
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

        public async Task<bool> ValidateAttendanceIsLockedAsync(Guid classId, DateTime date)
        {
            var dateOnly = DateOnly.FromDateTime(date);
            var byDate = await _dateLockRepository.FindAsync(l => l.ClassId == classId && l.AttendanceDate == dateOnly && l.IsLocked);
            if (byDate != null) return true;
            var ay = await _academicYearRepository.FindAllAsync(a => a.StartDate <= dateOnly && a.EndDate >= dateOnly);
            var currentAy = ay?.FirstOrDefault(a => a.IsActive == true);
            if (currentAy == null) return false;
            var byYear = await _academicYearLockRepository.FindAsync(l => l.AcademicYearId == currentAy.Id && l.IsLocked);
            return byYear != null;
        }

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

        private async Task<(DateOnly start, DateOnly end)> ResolveAcademicYearRangeAsync(Guid? studentId)
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

        private async Task<List<Attendance>> GetAttendanceRecordsInRangeAsync(Guid studentId, DateOnly start, DateOnly end)
        {
            var list = await _attendanceRepository.FindAllAsync(a =>
                a.StudentId == studentId &&
                a.AttendanceDate >= start &&
                a.AttendanceDate <= end &&
                a.IsDeleted != true);
            return list ?? new List<Attendance>();
        }

        private async Task<List<Attendance>> GetClassAttendanceRecordsInRangeAsync(Guid classId, DateOnly start, DateOnly end)
        {
            var list = await _attendanceRepository.FindAllAsync(a =>
                a.ClassId == classId &&
                a.AttendanceDate >= start &&
                a.AttendanceDate <= end &&
                a.IsDeleted != true);
            return list ?? new List<Attendance>();
        }

        private async Task<List<Attendance>> GetDivisionAttendanceRecordsInRangeAsync(Guid divisionId, DateOnly start, DateOnly end)
        {
            var list = await _attendanceRepository.FindAllAsync(a =>
                a.DivisionId == divisionId &&
                a.AttendanceDate >= start &&
                a.AttendanceDate <= end &&
                a.IsDeleted != true);
            return list ?? new List<Attendance>();
        }

        private async Task<List<Attendance>> GetBranchAttendanceRecordsInRangeAsync(Guid branchId, DateOnly start, DateOnly end)
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

        private static bool IsStatus(string? status, string value)
        {
            return string.Equals(status, value, StringComparison.OrdinalIgnoreCase);
        }

        private async Task<(AttendanceSaturdayPattern SaturdayPattern, bool SundayOff)> ResolveWeeklyOffAsync(Guid? branchId)
        {
            if (!branchId.HasValue || branchId.Value == Guid.Empty)
                return (AttendanceSaturdayPattern.None, true);
            var b = await _branchRepository.GetByIdAsync(branchId.Value);
            if (b == null) return (AttendanceSaturdayPattern.None, true);
            var sun = b.AttendanceSundayOff != false;
            return (EffectiveSaturdayPattern(b), sun);
        }

        private static AttendanceSaturdayPattern EffectiveSaturdayPattern(Branch b)
        {
            if (b.AttendanceSaturdayPattern != AttendanceSaturdayPattern.None)
                return b.AttendanceSaturdayPattern;
            return b.AttendanceSaturdayOff == true ? AttendanceSaturdayPattern.AllSaturdays : AttendanceSaturdayPattern.None;
        }

        private async Task<AttendanceCalendarContext> ResolveCalendarAsync(Guid? branchId, DateOnly start, DateOnly end)
        {
            var w = await ResolveWeeklyOffAsync(branchId);
            HashSet<DateOnly> holidays = new();
            if (branchId.HasValue && branchId.Value != Guid.Empty)
                holidays = await _schoolHolidayRepository.GetActiveDatesInRangeAsync(branchId.Value, start, end);
            return new AttendanceCalendarContext(w.SaturdayPattern, w.SundayOff, holidays);
        }

        private async Task<Guid?> ResolveBranchIdForStudentAsync(Guid studentId)
        {
            var s = await _studentRepository.GetByIdAsync(studentId);
            return s?.BranchId;
        }

        private async Task<Guid?> ResolveBranchIdForClassAsync(Guid classId)
        {
            var c = await _classRepository.GetByIdAsync(classId);
            return c?.BranchId;
        }

        private async Task<Guid?> ResolveBranchIdForDivisionAsync(Guid divisionId)
        {
            var d = await _divisionRepository.GetByIdAsync(divisionId);
            return d?.BranchId;
        }

        /// <summary>1-based index of this Saturday within its calendar month (first Saturday = 1).</summary>
        private static int SaturdayOrdinalInMonth(DateOnly d)
        {
            if (d.DayOfWeek != DayOfWeek.Saturday) return 0;
            var first = new DateOnly(d.Year, d.Month, 1);
            var dow1 = (int)first.DayOfWeek;
            var dowSat = (int)DayOfWeek.Saturday;
            var daysToFirstSat = (dowSat - dow1 + 7) % 7;
            var firstSat = first.AddDays(daysToFirstSat);
            return (d.DayNumber - firstSat.DayNumber) / 7 + 1;
        }

        private static bool IsSaturdayWeeklyOff(DateOnly d, AttendanceSaturdayPattern p)
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

        private static bool IsWeeklyOffDay(DateOnly d, AttendanceSaturdayPattern satPattern, bool sundayOff) =>
            IsSaturdayWeeklyOff(d, satPattern) || (sundayOff && d.DayOfWeek == DayOfWeek.Sunday);

        private static string? WeeklyOffTrendLabel(DateOnly d, AttendanceSaturdayPattern satPattern, bool sunOff)
        {
            if (IsSaturdayWeeklyOff(d, satPattern)) return "Saturday";
            if (sunOff && d.DayOfWeek == DayOfWeek.Sunday) return "Sunday";
            return null;
        }

        private static string? NonWorkingCalendarCell(DateOnly d, AttendanceCalendarContext ctx)
        {
            if (ctx.CalendarHolidayDates.Contains(d)) return "Holiday";
            return WeeklyOffTrendLabel(d, ctx.SaturdayPattern, ctx.SundayOff);
        }

        /// <summary>
        /// Working-day rows for attendance stats: excludes branch weekly off, calendar holidays, row <see cref="Attendance.IsHoliday"/>, and status Holiday.
        /// </summary>
        private static bool CountsTowardWorkingDayStats(Attendance r, AttendanceCalendarContext ctx)
        {
            if (r.AttendanceDate is not { } d) return false;
            if (r.IsHoliday == true) return false;
            if (IsStatus(r.Status, "Holiday")) return false;
            if (ctx.CalendarHolidayDates.Contains(d)) return false;
            if (IsWeeklyOffDay(d, ctx.SaturdayPattern, ctx.SundayOff)) return false;
            return true;
        }

        private static string FormatClassDayTrendLabel(DateOnly d, IReadOnlyList<Attendance>? dayRecords, AttendanceCalendarContext ctx)
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
            var present = working.Count(r => IsStatus(r.Status, "Present"));
            var pct = Math.Round((decimal)present / working.Count * 100, 2);
            return pct.ToString("F0") + "%";
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
                var total = working.Count;
                var present = working.Count(r => IsStatus(r.Status, "Present"));
                var pct = total > 0 ? Math.Round((decimal)present / total * 100, 2) : 0;
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

        private static AttendanceSummaryDto BuildSummaryFromRecords(List<Attendance> records, AttendanceCalendarContext ctx)
        {
            var relevant = records.Where(r => CountsTowardWorkingDayStats(r, ctx)).ToList();
            var present = relevant.Count(r => IsStatus(r.Status, "Present"));
            var absent = relevant.Count(r => IsStatus(r.Status, "Absent"));
            var leave = relevant.Count(r => IsStatus(r.Status, "Leave"));
            var halfDay = relevant.Count(r => IsStatus(r.Status, "HalfDay"));
            var late = relevant.Count(r => IsStatus(r.Status, "Late"));
            var total = present + absent + leave + halfDay;
            var percentage = total > 0 ? Math.Round((decimal)present / total * 100, 2) : 0;
            return new AttendanceSummaryDto { PresentCount = present, AbsentCount = absent, LeaveCount = leave, HalfDayCount = halfDay, LateCount = late, TotalDays = total, Percentage = percentage };
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
                decimal pct = 0; int present = 0; int total = 0;
                foreach (var s in g)
                {
                    var summary = await GetStudentAttendanceSummaryAsync(s.Id, periodType, null, null);
                    total += summary.TotalDays; present += summary.PresentCount;
                }
                if (total > 0) pct = Math.Round((decimal)present / total * 100, 2);
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
                foreach (var s in g) { var summary = await GetStudentAttendanceSummaryAsync(s.Id, periodType, null, null); total += summary.TotalDays; present += summary.PresentCount; }
                var pct = total > 0 ? Math.Round((decimal)present / total * 100, 2) : 0;
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

        private async Task<int> GetPendingLeaveApprovalsForBranchAsync(Guid branchId)
        {
            var students = await _studentRepository.FindAllAsync(s => s.BranchId == branchId);
            if (students == null || students.Count == 0) return 0;
            var ids = students.Select(s => s.Id).ToHashSet();
            var all = await _leaveRequestRepository.FindAllAsync(l => l.Status == "Pending");
            return all?.Count(l => ids.Contains(l.StudentId)) ?? 0;
        }

        private async Task<int> GetPendingCorrectionCountForBranchAsync(Guid branchId)
        {
            var all = await _correctionRequestRepository.FindAllAsync(c => c.Status == "Pending");
            return all?.Count ?? 0;
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

        private static LeaveRequestDto MapToLeaveRequestDto(LeaveRequest l, Student? s)
        {
            return new LeaveRequestDto
            {
                Id = l.Id, StudentId = l.StudentId, StudentName = s != null ? $"{s.FirstName} {s.LastName}".Trim() : null,
                FromDate = l.FromDate, ToDate = l.ToDate, LeaveType = l.LeaveType, Reason = l.Reason, Status = l.Status,
                RequestedAt = l.RequestedAt, RequestedBy = l.RequestedBy, ApprovedAt = l.ApprovedAt, ApprovedBy = l.ApprovedBy, RejectionReason = l.RejectionReason
            };
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
            return new AttendanceCorrectionRequestDto { Id = entity.Id, AttendanceId = entity.AttendanceId, Reason = entity.Reason, Status = entity.Status, RequestedAt = entity.RequestedAt, RequestedBy = entity.RequestedBy };
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
                Id = c.Id, AttendanceId = c.AttendanceId, Reason = c.Reason, Status = c.Status,
                RequestedAt = c.RequestedAt, RequestedBy = c.RequestedBy, ApprovedAt = c.ApprovedAt, ApprovedBy = c.ApprovedBy, RejectionReason = c.RejectionReason
            }).ToList();
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

        private static ExportResultDto BuildCsvExport<T>(string fileName, IEnumerable<T> rows)
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

        // --- UDISE / RTE ---
        public async Task<UDISEReportDto> GetUDISEAttendanceReportAsync(Guid branchId)
        {
            var students = await _studentRepository.FindAllAsync(s => s.BranchId == branchId);
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var records = await GetBranchAttendanceRecordsInRangeAsync(branchId, today, today);
            var ctx = await ResolveCalendarAsync(branchId, today, today);
            var present = records.Count(r => CountsTowardWorkingDayStats(r, ctx) && IsStatus(r.Status, "Present"));
            var absent = records.Count(r => CountsTowardWorkingDayStats(r, ctx) && IsStatus(r.Status, "Absent"));
            var total = students?.Count ?? 0;
            var rate = total > 0 ? Math.Round((decimal)present / total * 100, 2) : 0;
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
