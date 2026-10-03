using API.Models;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Constants;
using Application.Common.Extensions;
using Application.Common.Helpers;
using Application.ViewModels.Attendance;
using Domain.Entities.Postgres.Scaffolded;
using Infrastructure;
using Infrastructure.Security.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/attendance")]
    [Route("api/attendance/operations")]
    public class AttendanceOperationsController : ControllerBase
    {
        private readonly IAttendanceService _service;
        private readonly ILogger<AttendanceOperationsController> _logger;
        private readonly IStaffRepository _staffRepository;
        private readonly IClassTeacherMapRepository _classTeacherMapRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public AttendanceOperationsController(
            IAttendanceService service,
            ILogger<AttendanceOperationsController> logger,
            IStaffRepository staffRepository,
            IClassTeacherMapRepository classTeacherMapRepository,
            UserManager<ApplicationUser> userManager)
        {
            _service = service;
            _logger = logger;
            _staffRepository = staffRepository;
            _classTeacherMapRepository = classTeacherMapRepository;
            _userManager = userManager;
        }

        [HttpPost("mark")]
        public async Task<IActionResult> MarkAttendance([FromBody] MarkAttendanceDto dto)
        {
            _logger.LogInformation("MarkAttendance");
            var result = await _service.MarkAttendanceAsync(dto);
            await _service.CommitAsync();
            return Ok(result);
        }

        [HttpPost("bulk")]
        public async Task<IActionResult> BulkMarkAttendance([FromBody] BulkAttendanceDto dto)
        {
            _logger.LogInformation("BulkMarkAttendance");
            var result = await _service.BulkMarkAttendanceAsync(dto);
            return Ok(result);
        }

        [HttpGet("bulk-import/sample")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public IActionResult DownloadBulkAttendanceImportSample()
        {
            var bytes = BulkAttendanceImportFileParser.CreateSampleWorkbookBytes();
            const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            return File(bytes, contentType, "bulk-attendance-sample.xlsx");
        }

        [HttpPost("bulk-import/division-template")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> DownloadDivisionAttendanceImportTemplate([FromBody] AttendanceDivisionTemplateRequest body)
        {
            if (body == null)
                return BadRequest(new { message = "Request body required." });

            var bytes = await _service.GenerateDivisionAttendanceImportTemplateAsync(body);
            var safe = $"attendance-import-{body.ClassId:N}-{body.DivisionId:N}.xlsx";
            const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            return File(bytes, contentType, safe);
        }

        [Authorize]
        [HttpPost("bulk-import/preview")]
        public async Task<IActionResult> PreviewBulkAttendanceImport([FromBody] BulkAttendanceImportRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Request body required." });

            var teacherError = await ApplyResolvedClassTeacherAsync(request);
            if (teacherError != null)
                return teacherError;

            var result = await _service.PreviewBulkAttendanceImportAsync(request);
            return Ok(result);
        }

        [Authorize]
        [HttpPost("bulk-import/preview/file")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(25 * 1024 * 1024)]
        public async Task<IActionResult> PreviewBulkAttendanceImportFromFile([FromForm] BulkAttendanceImportUploadForm form)
        {
            if (form.BranchId == Guid.Empty)
                return BadRequest(new { message = "BranchId is required." });
            if (form.File == null || form.File.Length == 0)
                return BadRequest(new { message = "File is required (.xlsx, .xls, or .csv)." });

            List<BulkAttendanceImportRowInput> rows;
            try
            {
                await using var stream = form.File.OpenReadStream();
                rows = BulkAttendanceImportFileParser.ParseToRows(stream, form.File.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Bulk attendance preview: failed to parse file {Name}", form.File.FileName);
                return BadRequest(new { message = "Could not read the file.", detail = ex.Message });
            }

            if (rows.Count == 0)
                return BadRequest(new { message = "No data rows found after the header." });

            var teacherResult = await ResolveBulkImportClassTeacherIdAsync(form.BranchId, form.ClassId, form.DivisionId, form.ClassTeacherId);
            if (teacherResult.Error != null)
                return teacherResult.Error;

            var request = BuildBulkImportRequestFromForm(form, rows, teacherResult.ClassTeacherId!.Value);
            var result = await _service.PreviewBulkAttendanceImportAsync(request);
            return Ok(result);
        }

        [Authorize]
        [HttpPost("bulk-import")]
        public async Task<IActionResult> BulkImportAttendanceFromMachine([FromBody] BulkAttendanceImportRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Request body required." });

            var teacherError = await ApplyResolvedClassTeacherAsync(request);
            if (teacherError != null)
                return teacherError;

            var result = await _service.ImportBulkAttendanceFromMachineAsync(request);
            return Ok(result);
        }

        [Authorize]
        [HttpPost("bulk-import/file")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(25 * 1024 * 1024)]
        public async Task<IActionResult> BulkImportAttendanceFromFile([FromForm] BulkAttendanceImportUploadForm form)
        {
            if (form.BranchId == Guid.Empty)
                return BadRequest(new { message = "BranchId is required." });
            if (form.File == null || form.File.Length == 0)
                return BadRequest(new { message = "File is required (.xlsx, .xls, or .csv)." });

            List<BulkAttendanceImportRowInput> rows;
            try
            {
                await using var stream = form.File.OpenReadStream();
                rows = BulkAttendanceImportFileParser.ParseToRows(stream, form.File.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Bulk attendance import: failed to parse file {Name}", form.File.FileName);
                return BadRequest(new { message = "Could not read the file.", detail = ex.Message });
            }

            if (rows.Count == 0)
                return BadRequest(new { message = "No data rows found after the header." });

            var teacherResult = await ResolveBulkImportClassTeacherIdAsync(form.BranchId, form.ClassId, form.DivisionId, form.ClassTeacherId);
            if (teacherResult.Error != null)
                return teacherResult.Error;

            var request = BuildBulkImportRequestFromForm(form, rows, teacherResult.ClassTeacherId!.Value);
            var result = await _service.ImportBulkAttendanceFromMachineAsync(request);
            return Ok(result);
        }

        [Authorize]
        [HttpPost("bulk-import/division-template/preview-file")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(25 * 1024 * 1024)]
        public async Task<IActionResult> PreviewDivisionTemplateAttendanceImportFromFile([FromForm] BulkAttendanceImportUploadForm form)
        {
            var templateError = ValidateDivisionTemplateUploadForm(form);
            if (templateError != null)
                return templateError;
            return await PreviewBulkAttendanceImportFromFile(form);
        }

        [Authorize]
        [HttpPost("bulk-import/division-template/import-file")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(25 * 1024 * 1024)]
        public async Task<IActionResult> ImportDivisionTemplateAttendanceFromFile([FromForm] BulkAttendanceImportUploadForm form)
        {
            var templateError = ValidateDivisionTemplateUploadForm(form);
            if (templateError != null)
                return templateError;
            return await BulkImportAttendanceFromFile(form);
        }

        [HttpPost("class/{classId}/{divisionId}")]
        public async Task<IActionResult> MarkClassAttendance(Guid classId, Guid divisionId, [FromQuery] DateTime date, [FromBody] List<StudentAttendanceDto> students)
        {
            _logger.LogInformation("MarkClassAttendance ClassId={ClassId}, DivisionId={DivisionId}", classId, divisionId);
            var result = await _service.MarkClassAttendanceAsync(classId, divisionId, date, students ?? new List<StudentAttendanceDto>());
            return Ok(result);
        }

        [HttpPost("lock/date")]
        public async Task<IActionResult> LockAttendanceByDate([FromQuery] Guid classId, [FromQuery] Guid divisionId, [FromQuery] DateTime date)
        {
            await _service.LockAttendanceByDateAsync(classId, divisionId, date);
            await _service.CommitAsync();
            return NoContent();
        }

        [HttpPost("unlock/date")]
        public async Task<IActionResult> UnlockAttendanceByDate([FromQuery] Guid classId, [FromQuery] Guid divisionId, [FromQuery] DateTime date)
        {
            await _service.UnlockAttendanceByDateAsync(classId, divisionId, date);
            await _service.CommitAsync();
            return NoContent();
        }

        [HttpPost("lock/academic-year/{academicYearId}")]
        public async Task<IActionResult> LockAttendanceForAcademicYear(Guid academicYearId)
        {
            await _service.LockAttendanceForAcademicYearAsync(academicYearId);
            await _service.CommitAsync();
            return NoContent();
        }

        [HttpPost("unlock/academic-year/{academicYearId}")]
        public async Task<IActionResult> UnlockAttendanceForAcademicYear(Guid academicYearId)
        {
            await _service.UnlockAttendanceForAcademicYearAsync(academicYearId);
            await _service.CommitAsync();
            return NoContent();
        }

        [HttpGet("lock/validate")]
        public async Task<IActionResult> ValidateAttendanceIsLocked([FromQuery] Guid classId, [FromQuery] DateTime date)
        {
            var result = await _service.ValidateAttendanceIsLockedAsync(classId, date);
            return Ok(new { IsLocked = result });
        }

        [HttpGet("resolve-date-range")]
        public async Task<IActionResult> ResolveDateRange([FromQuery] AttendancePeriodType periodType, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] Guid? studentId)
        {
            var result = await _service.ResolveDateRangeAsync(periodType, startDate, endDate, studentId);
            return Ok(result);
        }

        [HttpGet("validate/not-duplicate")]
        public async Task<IActionResult> ValidateAttendanceNotDuplicate([FromQuery] Guid studentId, [FromQuery] DateTime date)
        {
            var result = await _service.ValidateAttendanceNotDuplicateAsync(studentId, date);
            return Ok(result);
        }

        [HttpGet("validate/within-academic-year")]
        public async Task<IActionResult> ValidateAttendanceWithinAcademicYear([FromQuery] DateTime date, [FromQuery] Guid academicYearId)
        {
            var result = await _service.ValidateAttendanceWithinAcademicYearAsync(date, academicYearId);
            return Ok(result);
        }

        [HttpGet("validate/student-active/{studentId}")]
        public async Task<IActionResult> ValidateStudentActive(Guid studentId)
        {
            var result = await _service.ValidateStudentActiveAsync(studentId);
            return Ok(result);
        }

        [HttpGet("validate/student-class")]
        public async Task<IActionResult> ValidateStudentBelongsToClass([FromQuery] Guid studentId, [FromQuery] Guid classId)
        {
            var result = await _service.ValidateStudentBelongsToClassAsync(studentId, classId);
            return Ok(result);
        }

        [HttpGet("validate/division-match")]
        public async Task<IActionResult> ValidateDivisionMatch([FromQuery] Guid studentId, [FromQuery] Guid divisionId)
        {
            var result = await _service.ValidateDivisionMatchAsync(studentId, divisionId);
            return Ok(result);
        }

        [HttpGet("validate/holiday")]
        public async Task<IActionResult> ValidateHoliday([FromQuery] DateTime date, [FromQuery] Guid? branchId = null)
        {
            var result = await _service.ValidateHolidayAsync(date, branchId);
            return Ok(result);
        }

        [HttpGet("validate/period")]
        public async Task<IActionResult> ValidateAttendancePeriod([FromQuery] AttendancePeriodType periodType, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var result = await _service.ValidateAttendancePeriodAsync(periodType, startDate, endDate);
            return Ok(result);
        }

        [HttpPost("leave/apply")]
        public async Task<IActionResult> ApplyStudentLeave([FromBody] ApplyLeaveDto dto)
        {
            var result = await _service.ApplyStudentLeaveAsync(dto.StudentId, dto.FromDate, dto.ToDate, dto.LeaveType, dto.Reason);
            await _service.CommitAsync();
            return Ok(result);
        }

        [HttpPost("leave/{leaveId}/approve")]
        public async Task<IActionResult> ApproveLeaveRequest(Guid leaveId, [FromQuery] Guid? approvedBy)
        {
            var success = await _service.ApproveLeaveRequestAsync(leaveId, approvedBy);
            if (!success) return NotFound();
            await _service.CommitAsync();
            return NoContent();
        }

        [HttpPost("leave/{leaveId}/reject")]
        public async Task<IActionResult> RejectLeaveRequest(Guid leaveId, [FromQuery] string? rejectionReason, [FromQuery] Guid? rejectedBy)
        {
            var success = await _service.RejectLeaveRequestAsync(leaveId, rejectionReason, rejectedBy);
            if (!success) return NotFound();
            await _service.CommitAsync();
            return NoContent();
        }

        [HttpGet("leave/student/{studentId}/history")]
        public async Task<IActionResult> GetStudentLeaveHistory(Guid studentId)
        {
            var result = await _service.GetStudentLeaveHistoryAsync(studentId);
            return Ok(result);
        }

        [HttpGet("leave/class/{classId}/pending")]
        public async Task<IActionResult> GetPendingLeaveApprovals(Guid classId)
        {
            var result = await _service.GetPendingLeaveApprovalsAsync(classId);
            return Ok(result);
        }

        [HttpPost("correction/request")]
        public async Task<IActionResult> RequestAttendanceCorrection([FromQuery] Guid attendanceId, [FromQuery] string reason, [FromQuery] Guid? requestedBy)
        {
            var result = await _service.RequestAttendanceCorrectionAsync(attendanceId, reason, requestedBy);
            await _service.CommitAsync();
            return Ok(result);
        }

        [HttpPost("correction/{requestId}/approve")]
        public async Task<IActionResult> ApproveAttendanceCorrection(Guid requestId, [FromQuery] Guid? approvedBy)
        {
            var success = await _service.ApproveAttendanceCorrectionAsync(requestId, approvedBy);
            if (!success) return NotFound();
            await _service.CommitAsync();
            return NoContent();
        }

        [HttpPost("correction/{requestId}/reject")]
        public async Task<IActionResult> RejectAttendanceCorrection(Guid requestId, [FromQuery] string? rejectionReason, [FromQuery] Guid? rejectedBy)
        {
            var success = await _service.RejectAttendanceCorrectionAsync(requestId, rejectionReason, rejectedBy);
            if (!success) return NotFound();
            await _service.CommitAsync();
            return NoContent();
        }

        [HttpGet("correction/class/{classId}")]
        public async Task<IActionResult> GetAttendanceCorrectionRequests(Guid classId)
        {
            var result = await _service.GetAttendanceCorrectionRequestsAsync(classId);
            return Ok(result);
        }

        [HttpPost("audit/{attendanceId}")]
        public async Task<IActionResult> AuditAttendanceChange(Guid attendanceId, [FromQuery] string action, [FromQuery] string? details)
        {
            await _service.AuditAttendanceChangeAsync(attendanceId, action, details);
            return NoContent();
        }

        [HttpPost("notify/absence")]
        public async Task<IActionResult> NotifyParentOnAbsence([FromQuery] Guid studentId, [FromQuery] DateTime date)
        {
            await _service.NotifyParentOnAbsenceAsync(studentId, date);
            return NoContent();
        }

        [HttpPost("notify/consecutive-absence")]
        public async Task<IActionResult> NotifyParentOnConsecutiveAbsence([FromQuery] Guid studentId)
        {
            await _service.NotifyParentOnConsecutiveAbsenceAsync(studentId);
            return NoContent();
        }

        [HttpPost("notify/low-attendance")]
        public async Task<IActionResult> NotifyClassTeacherLowAttendance([FromQuery] Guid studentId)
        {
            await _service.NotifyClassTeacherLowAttendanceAsync(studentId);
            return NoContent();
        }

        [HttpPost("notify/chronic-absentee")]
        public async Task<IActionResult> NotifyPrincipalChronicAbsentee([FromQuery] Guid branchId)
        {
            await _service.NotifyPrincipalChronicAbsenteeAsync(branchId);
            return NoContent();
        }

        [HttpPost("notify/daily-absence")]
        public async Task<IActionResult> SendDailyAbsenceNotification([FromQuery] Guid branchId)
        {
            await _service.SendDailyAbsenceNotificationAsync(branchId);
            return NoContent();
        }

        [HttpPost("auto/holiday")]
        public async Task<IActionResult> AutoMarkHolidayAttendance([FromQuery] DateTime date)
        {
            await _service.AutoMarkHolidayAttendanceAsync(date);
            return NoContent();
        }

        [HttpPost("auto/close/{classId}")]
        public async Task<IActionResult> AutoCloseAttendanceAfterTime(Guid classId)
        {
            await _service.AutoCloseAttendanceAfterTimeAsync(classId);
            return NoContent();
        }

        [HttpPost("auto/calculate-percentages")]
        public async Task<IActionResult> AutoCalculateAttendancePercentages([FromQuery] Guid academicYearId)
        {
            await _service.AutoCalculateAttendancePercentagesAsync(academicYearId);
            return NoContent();
        }

        [HttpPost("auto/flag-low-attendance")]
        public async Task<IActionResult> AutoFlagLowAttendanceStudents([FromQuery] decimal threshold)
        {
            await _service.AutoFlagLowAttendanceStudentsAsync(threshold);
            return NoContent();
        }

        [HttpPost("auto/notify-chronic-absentees")]
        public async Task<IActionResult> AutoNotifyChronicAbsentees()
        {
            await _service.AutoNotifyChronicAbsenteesAsync();
            return NoContent();
        }

        [HttpPost("auto/recalculate")]
        public async Task<IActionResult> RecalculateAttendanceStatistics([FromQuery] Guid academicYearId)
        {
            await _service.RecalculateAttendanceStatisticsAsync(academicYearId);
            return NoContent();
        }

        [HttpPost("auto/freeze")]
        public async Task<IActionResult> FreezeAttendanceForAcademicYear([FromQuery] Guid academicYearId)
        {
            var result = await _service.FreezeAttendanceForAcademicYearAsync(academicYearId);
            return Ok(new { Frozen = result });
        }

        [HttpGet("auto/validate")]
        public async Task<IActionResult> ValidateAttendanceIntegrity([FromQuery] Guid academicYearId)
        {
            var result = await _service.ValidateAttendanceIntegrityAsync(academicYearId);
            return Ok(result);
        }

        private static IActionResult? ValidateDivisionTemplateUploadForm(BulkAttendanceImportUploadForm form)
        {
            if (!form.ClassId.HasValue || form.ClassId.Value == Guid.Empty)
                return new BadRequestObjectResult(new { message = "ClassId is required for division template upload." });
            if (!form.DivisionId.HasValue || form.DivisionId.Value == Guid.Empty)
                return new BadRequestObjectResult(new { message = "DivisionId is required for division template upload." });
            return null;
        }

        private BulkAttendanceImportRequest BuildBulkImportRequestFromForm(
            BulkAttendanceImportUploadForm form,
            List<BulkAttendanceImportRowInput> rows,
            Guid classTeacherStaffId)
        {
            DateOnly? defaultDate = null;
            if (!string.IsNullOrWhiteSpace(form.DefaultDate) &&
                DateOnly.TryParse(form.DefaultDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dd))
                defaultDate = dd;

            return new BulkAttendanceImportRequest
            {
                BranchId = form.BranchId,
                ClassId = form.ClassId,
                DivisionId = form.DivisionId,
                ClassTeacherId = classTeacherStaffId,
                DefaultDate = defaultDate,
                DefaultPresentWhenStatusEmpty = form.DefaultPresentWhenStatusEmpty,
                Rows = rows
            };
        }

        private async Task<IActionResult?> ApplyResolvedClassTeacherAsync(BulkAttendanceImportRequest request)
        {
            var result = await ResolveBulkImportClassTeacherIdAsync(request.BranchId, request.ClassId, request.DivisionId, request.ClassTeacherId);
            if (result.Error != null)
                return result.Error;
            request.ClassTeacherId = result.ClassTeacherId;
            return null;
        }

        private sealed record BulkImportTeacherResult(Guid? ClassTeacherId, IActionResult? Error);

        private async Task<BulkImportTeacherResult> ResolveBulkImportClassTeacherIdAsync(
            Guid branchId,
            Guid? classId,
            Guid? divisionId,
            Guid? formClassTeacherId)
        {
            var isElevated = IsElevatedAttendanceUser();
            var staffIdFromLogin = await ResolveStaffIdFromCurrentUserAsync();

            Guid? effective = null;
            if (isElevated && formClassTeacherId.HasValue && formClassTeacherId.Value != Guid.Empty)
                effective = formClassTeacherId.Value;
            else if (staffIdFromLogin.HasValue)
                effective = staffIdFromLogin.Value;
            else if (isElevated)
                effective = formClassTeacherId;

            if ((!effective.HasValue || effective.Value == Guid.Empty) && isElevated &&
                classId.HasValue && classId.Value != Guid.Empty)
            {
                if (divisionId.HasValue && divisionId.Value != Guid.Empty)
                    effective = await ResolveStaffIdFromClassTeacherMapAsync(classId.Value, divisionId.Value);

                if (!effective.HasValue || effective.Value == Guid.Empty)
                    effective = await ResolveStaffIdFromClassTeacherMapForClassAsync(classId.Value);
            }

            if (!effective.HasValue || effective.Value == Guid.Empty)
            {
                return new BulkImportTeacherResult(null, BadRequest(new
                {
                    message = "Could not determine class teacher for attendance. Link your login email to Staff.Email, configure Class Teacher Map for this class, or submit ClassTeacherId (staff id) if you are an administrator.",
                }));
            }

            var staff = await _staffRepository.FindAsync(s => s.Id == effective.Value);
            if (staff == null)
                return new BulkImportTeacherResult(null, BadRequest(new { message = "Class teacher staff record was not found." }));

            if (staff.BranchId.HasValue && staff.BranchId.Value != branchId)
            {
                return new BulkImportTeacherResult(null, BadRequest(new
                {
                    message = "Selected branch does not match the staff member's branch.",
                }));
            }

            var hasClassDivision =
                classId.HasValue && classId.Value != Guid.Empty &&
                divisionId.HasValue && divisionId.Value != Guid.Empty;

            if (!isElevated && hasClassDivision)
            {
                var allowed = await IsStaffAssignedClassTeacherAsync(effective.Value, classId!.Value, divisionId!.Value);
                if (!allowed)
                {
                    return new BulkImportTeacherResult(null, StatusCode(StatusCodes.Status403Forbidden, new
                    {
                        message = "You are not assigned as class teacher for the selected class and division. Contact an administrator to update class teacher mapping.",
                    }));
                }
            }

            return new BulkImportTeacherResult(effective.Value, null);
        }

        private bool IsElevatedAttendanceUser()
        {
            foreach (var role in new[] { "Admin", Roles.Admin, "SuperAdmin", "Administrator" })
            {
                if (User.IsInRole(role))
                    return true;
            }

            return false;
        }

        private async Task<Guid?> ResolveStaffIdFromCurrentUserAsync()
        {
            var email = User.GetUserEmail()
                ?? User.FindFirstValue(ClaimTypes.Email)
                ?? User.FindFirstValue("email");
            if (string.IsNullOrWhiteSpace(email))
            {
                var userId = User.GetUserId() ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return null;
                var appUser = await _userManager.FindByIdAsync(userId);
                email = appUser?.Email;
            }
            if (string.IsNullOrWhiteSpace(email))
                return null;

            var trimmed = email.Trim();
            var list = await _staffRepository.FindAllAsync(s =>
                s.Email != null && (s.IsActive == null || s.IsActive == true));
            var match = list?.FirstOrDefault(s =>
                string.Equals(s.Email?.Trim(), trimmed, StringComparison.OrdinalIgnoreCase));
            return match?.Id;
        }

        private async Task<bool> IsStaffAssignedClassTeacherAsync(Guid staffId, Guid classId, Guid divisionId)
        {
            var maps = await _classTeacherMapRepository.FindAllAsync(m =>
                m.StaffId == staffId &&
                m.ClassId == classId &&
                (m.DivisionId == null || m.DivisionId == divisionId));
            return maps != null && maps.Count > 0;
        }

        private async Task<Guid?> ResolveStaffIdFromClassTeacherMapAsync(Guid classId, Guid divisionId)
        {
            var maps = await _classTeacherMapRepository.FindAllAsync(m =>
                m.ClassId == classId && (m.DivisionId == null || m.DivisionId == divisionId));
            var list = maps?.ToList() ?? new List<ClassTeacherMap>();
            var map = list.FirstOrDefault(m => m.DivisionId == divisionId) ?? list.FirstOrDefault();
            if (map?.StaffId == null || map.StaffId == Guid.Empty)
                return null;
            return map.StaffId.Value;
        }

        private async Task<Guid?> ResolveStaffIdFromClassTeacherMapForClassAsync(Guid classId)
        {
            var maps = await _classTeacherMapRepository.FindAllAsync(m => m.ClassId == classId);
            var map = maps?.FirstOrDefault(m => m.StaffId.HasValue && m.StaffId.Value != Guid.Empty);
            if (map?.StaffId == null || map.StaffId == Guid.Empty)
                return null;
            return map.StaffId.Value;
        }
    }
}
