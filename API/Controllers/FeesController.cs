using System;
using System.Threading.Tasks;
using Application.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FeesController : ControllerBase
{
    private readonly IFeeStudentService _studentService;
    private readonly IFeeAnalyticsService _analyticsService;

    public FeesController(
        IFeeStudentService studentService,
        IFeeAnalyticsService analyticsService)
    {
        _studentService = studentService;
        _analyticsService = analyticsService;
    }

    [HttpGet("student-dashboard")]
    public async Task<IActionResult> GetStudentDashboard([FromQuery] Guid studentId, [FromQuery] Guid academicYearId)
    {
        var result = await _studentService.GetStudentDashboard(studentId, academicYearId);
        return Ok(result);
    }

    [HttpGet("student-dashboard-by-roll")]
    public async Task<IActionResult> GetStudentDashboardByRoll(
        [FromQuery] Guid branchId,
        [FromQuery] Guid classId,
        [FromQuery] string rollNumber,
        [FromQuery] Guid academicYearId)
    {
        var result = await _studentService.GetStudentDashboardByRoll(branchId, classId, rollNumber, academicYearId);
        return Ok(result);
    }

    [HttpGet("class-dashboard")]
    public async Task<IActionResult> GetClassDashboard([FromQuery] Guid branchId, [FromQuery] Guid classId, [FromQuery] Guid academicYearId)
    {
        var result = await _analyticsService.GetClassDashboard(branchId, classId, academicYearId);
        return Ok(result);
    }

    [HttpGet("division-dashboard")]
    public async Task<IActionResult> GetDivisionDashboard([FromQuery] Guid branchId, [FromQuery] Guid divisionId, [FromQuery] Guid academicYearId)
    {
        var result = await _analyticsService.GetDivisionDashboard(branchId, divisionId, academicYearId);
        return Ok(result);
    }

    [HttpGet("branch-dashboard")]
    public async Task<IActionResult> GetBranchDashboard([FromQuery] Guid branchId, [FromQuery] Guid academicYearId)
    {
        var result = await _analyticsService.GetBranchDashboard(branchId, academicYearId);
        return Ok(result);
    }

    [HttpGet("collection-trend")]
    public async Task<IActionResult> GetCollectionTrend([FromQuery] Guid branchId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        var result = await _analyticsService.GetCollectionTrend(branchId, startDate, endDate);
        return Ok(result);
    }

    [HttpGet("feehead-analytics")]
    public async Task<IActionResult> GetFeeHeadAnalytics(
        [FromQuery] Guid branchId,
        [FromQuery] Guid academicYearId,
        [FromQuery] Guid? classId,
        [FromQuery] Guid? divisionId)
    {
        var result = await _analyticsService.GetFeeHeadAnalytics(branchId, academicYearId, classId, divisionId);
        return Ok(result);
    }

    [HttpGet("installment-analytics")]
    public async Task<IActionResult> GetInstallmentAnalytics(
        [FromQuery] Guid branchId,
        [FromQuery] Guid academicYearId,
        [FromQuery] Guid? classId,
        [FromQuery] Guid? divisionId)
    {
        var result = await _analyticsService.GetInstallmentAnalytics(branchId, academicYearId, classId, divisionId);
        return Ok(result);
    }

    [HttpGet("outstanding-by-student")]
    public async Task<IActionResult> GetOutstandingByStudent(
        [FromQuery] Guid branchId,
        [FromQuery] Guid academicYearId,
        [FromQuery] Guid? classId,
        [FromQuery] Guid? divisionId)
    {
        var result = await _analyticsService.GetOutstandingByStudent(branchId, classId, divisionId, academicYearId);
        return Ok(result);
    }

    [HttpGet("outstanding-by-class")]
    public async Task<IActionResult> GetOutstandingByClass(
        [FromQuery] Guid branchId,
        [FromQuery] Guid academicYearId)
    {
        var result = await _analyticsService.GetOutstandingByClass(branchId, academicYearId);
        return Ok(result);
    }

    [HttpGet("outstanding-by-division")]
    public async Task<IActionResult> GetOutstandingByDivision(
        [FromQuery] Guid branchId,
        [FromQuery] Guid academicYearId)
    {
        var result = await _analyticsService.GetOutstandingByDivision(branchId, academicYearId);
        return Ok(result);
    }

    [HttpGet("outstanding-by-branch")]
    public async Task<IActionResult> GetOutstandingByBranch([FromQuery] Guid academicYearId)
    {
        var result = await _analyticsService.GetOutstandingByBranch(academicYearId);
        return Ok(result);
    }

    [HttpGet("feehead-students")]
    public async Task<IActionResult> GetFeeHeadStatus(
        [FromQuery] Guid feeHeadId,
        [FromQuery] Guid branchId,
        [FromQuery] Guid academicYearId,
        [FromQuery] Guid? classId,
        [FromQuery] Guid? divisionId)
    {
        var result = await _analyticsService.GetFeeHeadStatus(feeHeadId, branchId, academicYearId, classId, divisionId);
        return Ok(result);
    }
}

