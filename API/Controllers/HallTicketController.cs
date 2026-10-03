using Application.Abstractions.Services;
using Application.ViewModels.Certificates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HallTicketController : ControllerBase
{
    private readonly IHallTicketService _service;
    private readonly ILogger<HallTicketController> _logger;

    public HallTicketController(IHallTicketService service, ILogger<HallTicketController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] HallTicketRequest request)
    {
        _logger.LogInformation("HTTP POST /api/HallTicket/generate - student {StudentId}, exam {ExamId}", request.StudentId, request.ExamId);
        var result = await _service.GenerateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        _logger.LogInformation("HTTP GET /api/HallTicket/{Id}", id);
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("student/{studentId}/exam/{examId}")]
    public async Task<IActionResult> GetByStudentAndExam(Guid studentId, Guid examId)
    {
        _logger.LogInformation("HTTP GET /api/HallTicket/student/{StudentId}/exam/{ExamId}", studentId, examId);
        var result = await _service.GetByStudentAndExamAsync(studentId, examId);
        if (result == null) return NotFound();
        return Ok(result);
    }
}
