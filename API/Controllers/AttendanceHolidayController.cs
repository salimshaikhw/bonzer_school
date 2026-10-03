using System;
using System.Threading.Tasks;
using Application.Abstractions.Services;
using Application.ViewModels.Attendance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttendanceHolidayController : ControllerBase
{
    private readonly IAttendanceHolidayService _service;

    public AttendanceHolidayController(IAttendanceHolidayService service)
    {
        _service = service;
    }

    [HttpGet("branch/{branchId:guid}/weekly-off")]
    public async Task<IActionResult> GetWeeklyOff(Guid branchId)
    {
        var result = await _service.GetWeeklyOffSettingsAsync(branchId);
        return Ok(result);
    }

    [HttpPut("branch/{branchId:guid}/weekly-off")]
    public async Task<IActionResult> UpdateWeeklyOff(Guid branchId, [FromBody] UpdateAttendanceWeeklyOffSettingsRequest request)
    {
        await _service.UpdateWeeklyOffSettingsAsync(branchId, request);
        await _service.CommitAsync();
        return Ok(await _service.GetWeeklyOffSettingsAsync(branchId));
    }

    [HttpGet("branch/{branchId:guid}/holidays")]
    public async Task<IActionResult> ListHolidays(Guid branchId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var list = await _service.ListHolidaysAsync(branchId, from, to);
        return Ok(list);
    }

    [HttpPost("branch/{branchId:guid}/holidays")]
    public async Task<IActionResult> AddHoliday(Guid branchId, [FromBody] SchoolHolidayRequest request)
    {
        var created = await _service.AddHolidayAsync(branchId, request);
        await _service.CommitAsync();
        return CreatedAtAction(nameof(ListHolidays), new { branchId }, created);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteHoliday(Guid id)
    {
        var ok = await _service.DeleteHolidayAsync(id);
        if (!ok) return NotFound();
        await _service.CommitAsync();
        return NoContent();
    }
}
