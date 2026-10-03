using Application.Abstractions.Services;
using Application.ViewModels.Attendance;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/attendance")]
    [Route("api/attendance/analytics")]
    public class AttendanceAnalyticsController : ControllerBase
    {
        private readonly IAttendanceService _service;
        private readonly ILogger<AttendanceAnalyticsController> _logger;

        public AttendanceAnalyticsController(
            IAttendanceService service,
            ILogger<AttendanceAnalyticsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet("student/{studentId}/detailed")]
        public async Task<IActionResult> GetStudentAttendanceDetailedList(Guid studentId, [FromQuery] AttendancePeriodType periodType, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var result = await _service.GetStudentAttendanceDetailedListAsync(studentId, periodType, startDate, endDate);
            return Ok(result);
        }

        [HttpGet("student/{studentId}/percentage")]
        public async Task<IActionResult> GetStudentAttendancePercentage(Guid studentId, [FromQuery] AttendancePeriodType periodType)
        {
            var result = await _service.GetStudentAttendancePercentageAsync(studentId, periodType);
            return Ok(result);
        }

        [HttpGet("student/{studentId}/present-count")]
        public async Task<IActionResult> GetStudentPresentCount(Guid studentId, [FromQuery] AttendancePeriodType periodType)
        {
            var result = await _service.GetStudentPresentCountAsync(studentId, periodType);
            return Ok(result);
        }

        [HttpGet("student/{studentId}/absent-count")]
        public async Task<IActionResult> GetStudentAbsentCount(Guid studentId, [FromQuery] AttendancePeriodType periodType)
        {
            var result = await _service.GetStudentAbsentCountAsync(studentId, periodType);
            return Ok(result);
        }

        [HttpGet("student/{studentId}/leave-count")]
        public async Task<IActionResult> GetStudentLeaveCount(Guid studentId, [FromQuery] AttendancePeriodType periodType)
        {
            var result = await _service.GetStudentLeaveCountAsync(studentId, periodType);
            return Ok(result);
        }

        [HttpGet("student/{studentId}/halfday-count")]
        public async Task<IActionResult> GetStudentHalfDayCount(Guid studentId, [FromQuery] AttendancePeriodType periodType)
        {
            var result = await _service.GetStudentHalfDayCountAsync(studentId, periodType);
            return Ok(result);
        }

        [HttpGet("student/{studentId}/consecutive-absence-days")]
        public async Task<IActionResult> GetStudentConsecutiveAbsenceDays(Guid studentId)
        {
            var result = await _service.GetStudentConsecutiveAbsenceDaysAsync(studentId);
            return Ok(result);
        }

        [HttpGet("student/{studentId}/late-count")]
        public async Task<IActionResult> GetStudentLateCount(Guid studentId)
        {
            var result = await _service.GetStudentLateCountAsync(studentId);
            return Ok(result);
        }

        [HttpGet("student/{studentId}/is-low-attendance")]
        public async Task<IActionResult> IsStudentLowAttendance(Guid studentId, [FromQuery] decimal threshold)
        {
            var result = await _service.IsStudentLowAttendanceAsync(studentId, threshold);
            return Ok(new { IsLowAttendance = result });
        }

        [HttpGet("student/{studentId}/is-chronic-absentee")]
        public async Task<IActionResult> IsStudentChronicAbsentee(Guid studentId, [FromQuery] int consecutiveDays = 5)
        {
            var result = await _service.IsStudentChronicAbsenteeAsync(studentId, consecutiveDays);
            return Ok(new { IsChronicAbsentee = result });
        }

        [HttpGet("student/{studentId}/trend")]
        public async Task<IActionResult> GetStudentAttendanceTrend(Guid studentId, [FromQuery] AttendancePeriodType periodType)
        {
            var result = await _service.GetStudentAttendanceTrendAsync(studentId, periodType);
            return Ok(result);
        }

        [HttpGet("division/{divisionId}/report")]
        public async Task<IActionResult> GetDivisionAttendanceReport(Guid divisionId, [FromQuery] AttendancePeriodType periodType, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            _logger.LogInformation("GetDivisionAttendanceReport DivisionId={DivisionId}", divisionId);
            var result = await _service.GetDivisionAttendanceReportAsync(divisionId, periodType, startDate, endDate);
            return Ok(result);
        }

        [HttpGet("division/{divisionId}/summary")]
        public async Task<IActionResult> GetDivisionAttendanceSummary(Guid divisionId, [FromQuery] AttendancePeriodType periodType, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var result = await _service.GetDivisionAttendanceSummaryAsync(divisionId, periodType, startDate, endDate);
            return Ok(result);
        }

        [HttpGet("division/{divisionId}/detailed")]
        public async Task<IActionResult> GetDivisionAttendanceDetailedList(Guid divisionId, [FromQuery] AttendancePeriodType periodType, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var result = await _service.GetDivisionAttendanceDetailedListAsync(divisionId, periodType, startDate, endDate);
            return Ok(result);
        }

        [HttpGet("division/{divisionId}/percentage")]
        public async Task<IActionResult> GetDivisionAttendancePercentage(Guid divisionId, [FromQuery] AttendancePeriodType periodType)
        {
            var result = await _service.GetDivisionAttendancePercentageAsync(divisionId, periodType);
            return Ok(result);
        }

        [HttpGet("division/{divisionId}/present-count")]
        public async Task<IActionResult> GetDivisionPresentCount(Guid divisionId, [FromQuery] AttendancePeriodType periodType)
        {
            var result = await _service.GetDivisionPresentCountAsync(divisionId, periodType);
            return Ok(result);
        }

        [HttpGet("division/{divisionId}/absent-count")]
        public async Task<IActionResult> GetDivisionAbsentCount(Guid divisionId, [FromQuery] AttendancePeriodType periodType)
        {
            var result = await _service.GetDivisionAbsentCountAsync(divisionId, periodType);
            return Ok(result);
        }

        [HttpGet("division/{divisionId}/leave-count")]
        public async Task<IActionResult> GetDivisionLeaveCount(Guid divisionId, [FromQuery] AttendancePeriodType periodType)
        {
            var result = await _service.GetDivisionLeaveCountAsync(divisionId, periodType);
            return Ok(result);
        }

        [HttpGet("division/{divisionId}/low-attendance")]
        public async Task<IActionResult> GetDivisionLowAttendanceStudents(Guid divisionId, [FromQuery] decimal threshold)
        {
            var result = await _service.GetDivisionLowAttendanceStudentsAsync(divisionId, threshold);
            return Ok(result);
        }

        [HttpGet("division/{divisionId}/chronic-absentees")]
        public async Task<IActionResult> GetDivisionChronicAbsentees(Guid divisionId)
        {
            var result = await _service.GetDivisionChronicAbsenteesAsync(divisionId);
            return Ok(result);
        }

        [HttpGet("division/{divisionId}/trend")]
        public async Task<IActionResult> GetDivisionAttendanceTrend(Guid divisionId, [FromQuery] AttendancePeriodType periodType)
        {
            var result = await _service.GetDivisionAttendanceTrendAsync(divisionId, periodType);
            return Ok(result);
        }

        [HttpGet("branch/{branchId}/report")]
        public async Task<IActionResult> GetBranchAttendanceReport(Guid branchId, [FromQuery] AttendancePeriodType periodType, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var result = await _service.GetBranchAttendanceReportAsync(branchId, periodType, startDate, endDate);
            return Ok(result);
        }

        [HttpGet("branch/{branchId}/summary")]
        public async Task<IActionResult> GetBranchAttendanceSummary(Guid branchId, [FromQuery] AttendancePeriodType periodType)
        {
            var result = await _service.GetBranchAttendanceSummaryAsync(branchId, periodType);
            return Ok(result);
        }

        [HttpGet("branch/{branchId}/percentage")]
        public async Task<IActionResult> GetBranchAttendancePercentage(Guid branchId, [FromQuery] AttendancePeriodType periodType)
        {
            var result = await _service.GetBranchAttendancePercentageAsync(branchId, periodType);
            return Ok(result);
        }

        [HttpGet("branch/{branchId}/low-attendance")]
        public async Task<IActionResult> GetBranchLowAttendanceStudents(Guid branchId, [FromQuery] decimal threshold)
        {
            var result = await _service.GetBranchLowAttendanceStudentsAsync(branchId, threshold);
            return Ok(result);
        }

        [HttpGet("branch/{branchId}/chronic-absentees")]
        public async Task<IActionResult> GetBranchChronicAbsentees(Guid branchId)
        {
            var result = await _service.GetBranchChronicAbsenteesAsync(branchId);
            return Ok(result);
        }

        [HttpGet("branch/{branchId}/trend")]
        public async Task<IActionResult> GetBranchAttendanceTrend(Guid branchId, [FromQuery] AttendancePeriodType periodType)
        {
            var result = await _service.GetBranchAttendanceTrendAsync(branchId, periodType);
            return Ok(result);
        }

        [HttpGet("branch/{branchId}/comparison-classes")]
        public async Task<IActionResult> AttendanceComparisonBetweenClasses(Guid branchId, [FromQuery] AttendancePeriodType periodType)
        {
            var result = await _service.AttendanceComparisonBetweenClassesAsync(branchId, periodType);
            return Ok(result);
        }

        [HttpGet("branch/{branchId}/gender-distribution")]
        public async Task<IActionResult> GenderWiseAttendanceDistribution(Guid branchId, [FromQuery] AttendancePeriodType periodType)
        {
            var result = await _service.GenderWiseAttendanceDistributionAsync(branchId, periodType);
            return Ok(result);
        }

        [HttpGet("branch/{branchId}/category-distribution")]
        public async Task<IActionResult> CategoryWiseAttendanceDistribution(Guid branchId, [FromQuery] AttendancePeriodType periodType)
        {
            var result = await _service.CategoryWiseAttendanceDistributionAsync(branchId, periodType);
            return Ok(result);
        }

        [HttpGet("branch/{branchId}/today/summary")]
        public async Task<IActionResult> GetTodayAttendanceSummary(Guid branchId)
        {
            var result = await _service.GetTodayAttendanceSummaryAsync(branchId);
            return Ok(result);
        }

        [HttpGet("branch/{branchId}/today/present-count")]
        public async Task<IActionResult> GetTodayPresentCount(Guid branchId)
        {
            var result = await _service.GetTodayPresentCountAsync(branchId);
            return Ok(result);
        }

        [HttpGet("branch/{branchId}/today/absent-count")]
        public async Task<IActionResult> GetTodayAbsentCount(Guid branchId)
        {
            var result = await _service.GetTodayAbsentCountAsync(branchId);
            return Ok(result);
        }

        [HttpGet("branch/{branchId}/today/percentage")]
        public async Task<IActionResult> GetTodayAttendancePercentage(Guid branchId)
        {
            var result = await _service.GetTodayAttendancePercentageAsync(branchId);
            return Ok(result);
        }

        [HttpGet("branch/{branchId}/dashboard")]
        public async Task<IActionResult> GetAttendanceDashboardData(Guid branchId)
        {
            var result = await _service.GetAttendanceDashboardDataAsync(branchId);
            return Ok(result);
        }

        [HttpGet("export/student/{studentId}")]
        public async Task<IActionResult> ExportStudentAttendanceReport(Guid studentId, [FromQuery] AttendancePeriodType periodType)
        {
            var result = await _service.ExportStudentAttendanceReportAsync(studentId, periodType);
            return File(result.Content, result.ContentType, result.FileName);
        }

        [HttpGet("export/class/{classId}")]
        public async Task<IActionResult> ExportClassAttendanceReport(Guid classId, [FromQuery] AttendancePeriodType periodType)
        {
            var result = await _service.ExportClassAttendanceReportAsync(classId, periodType);
            return File(result.Content, result.ContentType, result.FileName);
        }

        [HttpGet("export/division/{divisionId}")]
        public async Task<IActionResult> ExportDivisionAttendanceReport(Guid divisionId, [FromQuery] AttendancePeriodType periodType)
        {
            var result = await _service.ExportDivisionAttendanceReportAsync(divisionId, periodType);
            return File(result.Content, result.ContentType, result.FileName);
        }

        [HttpGet("export/branch/{branchId}")]
        public async Task<IActionResult> ExportBranchAttendanceReport(Guid branchId, [FromQuery] AttendancePeriodType periodType)
        {
            var result = await _service.ExportBranchAttendanceReportAsync(branchId, periodType);
            return File(result.Content, result.ContentType, result.FileName);
        }

        [HttpGet("export/register/{classId}")]
        public async Task<IActionResult> GenerateAttendanceRegisterPDF(Guid classId, [FromQuery] int month, [FromQuery] int year)
        {
            var result = await _service.GenerateAttendanceRegisterPDFAsync(classId, month, year);
            return File(result.Content, result.ContentType, result.FileName);
        }

        [HttpGet("export/branch/{branchId}/excel")]
        public async Task<IActionResult> GenerateAttendanceExcel(Guid branchId, [FromQuery] AttendancePeriodType periodType)
        {
            var result = await _service.GenerateAttendanceExcelAsync(branchId, periodType);
            return File(result.Content, result.ContentType, result.FileName);
        }

        [HttpGet("udise/branch/{branchId}")]
        public async Task<IActionResult> GetUDISEAttendanceReport(Guid branchId)
        {
            var result = await _service.GetUDISEAttendanceReportAsync(branchId);
            return Ok(result);
        }

        [HttpGet("rte/branch/{branchId}")]
        public async Task<IActionResult> GetRTEAttendanceCompliance(Guid branchId)
        {
            var result = await _service.GetRTEAttendanceComplianceAsync(branchId);
            return Ok(result);
        }

        [HttpGet("student/{studentId}/report")]
        public async Task<IActionResult> GetStudentAttendanceReport(Guid studentId, [FromQuery] AttendancePeriodType periodType, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var result = await _service.GetStudentAttendanceReportAsync(studentId, periodType, startDate, endDate);
            return Ok(result);
        }

        [HttpGet("student/{studentId}/summary")]
        public async Task<IActionResult> GetStudentAttendanceSummary(Guid studentId, [FromQuery] AttendancePeriodType periodType, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var result = await _service.GetStudentAttendanceSummaryAsync(studentId, periodType, startDate, endDate);
            return Ok(result);
        }

        [HttpGet("class/{classId}/report")]
        public async Task<IActionResult> GetClassAttendanceReport(Guid classId, [FromQuery] AttendancePeriodType periodType, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var result = await _service.GetClassAttendanceReportAsync(classId, periodType, startDate, endDate);
            return Ok(result);
        }

        [HttpGet("class/{classId}/summary")]
        public async Task<IActionResult> GetClassAttendanceSummary(Guid classId, [FromQuery] AttendancePeriodType periodType, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var result = await _service.GetClassAttendanceSummaryAsync(classId, periodType, startDate, endDate);
            return Ok(result);
        }

        [HttpGet("class/{classId}/detailed")]
        public async Task<IActionResult> GetClassAttendanceDetailedList(Guid classId, [FromQuery] AttendancePeriodType periodType, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var result = await _service.GetClassAttendanceDetailedListAsync(classId, periodType, startDate, endDate);
            return Ok(result);
        }

        [HttpGet("class/{classId}/percentage")]
        public async Task<IActionResult> GetClassAttendancePercentage(Guid classId, [FromQuery] AttendancePeriodType periodType)
        {
            var result = await _service.GetClassAttendancePercentageAsync(classId, periodType);
            return Ok(result);
        }

        [HttpGet("class/{classId}/present-count")]
        public async Task<IActionResult> GetClassPresentCount(Guid classId, [FromQuery] AttendancePeriodType periodType)
        {
            var result = await _service.GetClassPresentCountAsync(classId, periodType);
            return Ok(result);
        }

        [HttpGet("class/{classId}/absent-count")]
        public async Task<IActionResult> GetClassAbsentCount(Guid classId, [FromQuery] AttendancePeriodType periodType)
        {
            var result = await _service.GetClassAbsentCountAsync(classId, periodType);
            return Ok(result);
        }

        [HttpGet("class/{classId}/leave-count")]
        public async Task<IActionResult> GetClassLeaveCount(Guid classId, [FromQuery] AttendancePeriodType periodType)
        {
            var result = await _service.GetClassLeaveCountAsync(classId, periodType);
            return Ok(result);
        }

        [HttpGet("class/{classId}/halfday-count")]
        public async Task<IActionResult> GetClassHalfDayCount(Guid classId, [FromQuery] AttendancePeriodType periodType)
        {
            var result = await _service.GetClassHalfDayCountAsync(classId, periodType);
            return Ok(result);
        }

        [HttpGet("class/{classId}/low-attendance")]
        public async Task<IActionResult> GetClassLowAttendanceStudents(Guid classId, [FromQuery] decimal threshold, [FromQuery] AttendancePeriodType periodType)
        {
            var result = await _service.GetClassLowAttendanceStudentsAsync(classId, threshold, periodType);
            return Ok(result);
        }

        [HttpGet("class/{classId}/chronic-absentees")]
        public async Task<IActionResult> GetClassChronicAbsentees(Guid classId, [FromQuery] int consecutiveDays = 5)
        {
            var result = await _service.GetClassChronicAbsenteesAsync(classId, consecutiveDays);
            return Ok(result);
        }

        [HttpGet("class/{classId}/trend")]
        public async Task<IActionResult> GetClassAttendanceTrend(Guid classId, [FromQuery] AttendancePeriodType periodType)
        {
            var result = await _service.GetClassAttendanceTrendAsync(classId, periodType);
            return Ok(result);
        }

        [HttpGet("class/{classId}/compare-months")]
        public async Task<IActionResult> CompareClassAttendanceAcrossMonths(Guid classId)
        {
            var result = await _service.CompareClassAttendanceAcrossMonthsAsync(classId);
            return Ok(result);
        }

        [HttpGet("class/{classId}/dropout-risk")]
        public async Task<IActionResult> PredictDropoutRiskForClass(Guid classId)
        {
            var result = await _service.PredictDropoutRiskForClassAsync(classId);
            return Ok(result);
        }

        [HttpGet("class/{classId}/attendance-vs-result")]
        public async Task<IActionResult> AttendanceVsResultAnalysisForClass(Guid classId)
        {
            var result = await _service.AttendanceVsResultAnalysisForClassAsync(classId);
            return Ok(result);
        }

        [HttpGet("class/{classId}/top-regular")]
        public async Task<IActionResult> GetTopRegularStudentsInClass(Guid classId)
        {
            var result = await _service.GetTopRegularStudentsInClassAsync(classId);
            return Ok(result);
        }

        [HttpGet("class/{classId}/heatmap")]
        public async Task<IActionResult> GetClassAttendanceHeatmap(Guid classId)
        {
            var result = await _service.GetClassAttendanceHeatmapAsync(classId);
            return Ok(result);
        }
    }
}
