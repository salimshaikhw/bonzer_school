using Application.Abstractions.Services;
using Application.ViewModels.Certificates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TcCertificateController : ControllerBase
{
    private readonly ITcCertificateService _service;
    private readonly ILogger<TcCertificateController> _logger;

    public TcCertificateController(ITcCertificateService service, ILogger<TcCertificateController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TcCertificateRequest request)
    {
        _logger.LogInformation("HTTP POST /api/TcCertificate - creating TC for student {StudentId}", request.StudentId);
        var result = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        _logger.LogInformation("HTTP GET /api/TcCertificate/{Id}", id);
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("student/{studentId}")]
    public async Task<IActionResult> GetByStudent(Guid studentId)
    {
        _logger.LogInformation("HTTP GET /api/TcCertificate/student/{StudentId}", studentId);
        var result = await _service.GetByStudentAsync(studentId);
        return Ok(result);
    }

    // ------------------------------------------------------------------
    // Workflow endpoints
    // ------------------------------------------------------------------

    // Step 1: student/parent requests TC and you create the TC row (POST /)
    // Step 2: initiate department clearances for this TC
    [HttpPost("{id:guid}/initiate")]
    public async Task<IActionResult> Initiate(Guid id)
    {
        _logger.LogInformation("HTTP POST /api/TcCertificate/{Id}/initiate", id);
        await _service.InitiateClearanceAsync(id);
        return NoContent();
    }

    // After all departments cleared (your UI calls check_tc_clearance_complete
    // via DB function or separately), the TC goes to Head Clerk.

    [HttpPost("{id:guid}/head-clerk-approve")]
    public async Task<IActionResult> HeadClerkApprove(Guid id, [FromQuery] Guid staffId)
    {
        _logger.LogInformation("HTTP POST /api/TcCertificate/{Id}/head-clerk-approve by {StaffId}", id, staffId);
        await _service.HeadClerkApproveAsync(id, staffId);
        return NoContent();
    }

    [HttpPost("{id:guid}/class-teacher-approve")]
    public async Task<IActionResult> ClassTeacherApprove(Guid id, [FromQuery] Guid staffId)
    {
        _logger.LogInformation("HTTP POST /api/TcCertificate/{Id}/class-teacher-approve by {StaffId}", id, staffId);
        await _service.ClassTeacherApproveAsync(id, staffId);
        return NoContent();
    }

    [HttpPost("{id:guid}/principal-approve")]
    public async Task<IActionResult> PrincipalApprove(Guid id, [FromQuery] Guid staffId)
    {
        _logger.LogInformation("HTTP POST /api/TcCertificate/{Id}/principal-approve by {StaffId}", id, staffId);
        await _service.PrincipalApproveAsync(id, staffId);
        return NoContent();
    }

    [HttpPost("{id:guid}/issue")]
    public async Task<IActionResult> Issue(Guid id, [FromQuery] Guid staffId)
    {
        _logger.LogInformation("HTTP POST /api/TcCertificate/{Id}/issue by {StaffId}", id, staffId);
        await _service.IssueAsync(id, staffId);
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromQuery] Guid staffId, [FromBody] string reason)
    {
        _logger.LogInformation("HTTP POST /api/TcCertificate/{Id}/reject by {StaffId}", id, staffId);
        await _service.RejectAsync(id, staffId, reason);
        return NoContent();
    }
}
