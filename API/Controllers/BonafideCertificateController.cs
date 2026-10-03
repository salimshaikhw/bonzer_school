using Application.Abstractions.Services;
using Application.ViewModels.Certificates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BonafideCertificateController : ControllerBase
{
    private readonly IBonafideCertificateService _service;
    private readonly ILogger<BonafideCertificateController> _logger;

    public BonafideCertificateController(IBonafideCertificateService service, ILogger<BonafideCertificateController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BonafideCertificateRequest request)
    {
        _logger.LogInformation("HTTP POST /api/BonafideCertificate - creating bonafide for student {StudentId}", request.StudentId);
        var result = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        _logger.LogInformation("HTTP GET /api/BonafideCertificate/{Id}", id);
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("student/{studentId}")]
    public async Task<IActionResult> GetByStudent(Guid studentId)
    {
        _logger.LogInformation("HTTP GET /api/BonafideCertificate/student/{StudentId}", studentId);
        var result = await _service.GetByStudentAsync(studentId);
        return Ok(result);
    }
}
