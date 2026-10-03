using Application.Abstractions.Services;
using Application.ViewModels.Attendance;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize]
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendanceService _service;
        private readonly ILogger<AttendanceController> _logger;

        public AttendanceController(
            IAttendanceService service,
            ILogger<AttendanceController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("GetAll attendances");
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            _logger.LogInformation("GetById attendance {Id}", id);
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AttendanceRequest request)
        {
            _logger.LogInformation("Create attendance");
            var result = await _service.CreateAsync(request);
            await _service.CommitAsync();
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] AttendanceRequest request)
        {
            _logger.LogInformation("Update attendance {Id}", id);
            var result = await _service.UpdateAsync(id, request);
            await _service.CommitAsync();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            _logger.LogInformation("Delete attendance {Id}", id);
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound();
            await _service.CommitAsync();
            return NoContent();
        }

        /// <summary>Update an existing attendance record (partial update).</summary>
        [HttpPatch("{attendanceId}")]
        public async Task<IActionResult> UpdateAttendance(Guid attendanceId, [FromBody] UpdateAttendanceDto dto)
        {
            _logger.LogInformation("UpdateAttendance {Id}", attendanceId);
            var result = await _service.UpdateAttendanceAsync(attendanceId, dto);
            if (result == null) return NotFound();
            await _service.CommitAsync();
            return Ok(result);
        }

        /// <summary>Soft delete an attendance record.</summary>
        [HttpDelete("{attendanceId}/soft")]
        public async Task<IActionResult> SoftDeleteAttendance(Guid attendanceId)
        {
            _logger.LogInformation("SoftDeleteAttendance {Id}", attendanceId);
            await _service.SoftDeleteAttendanceAsync(attendanceId);
            await _service.CommitAsync();
            return NoContent();
        }

        /// <summary>Permanently delete an attendance record.</summary>
        [HttpDelete("{attendanceId}/hard")]
        public async Task<IActionResult> HardDeleteAttendance(Guid attendanceId)
        {
            _logger.LogInformation("HardDeleteAttendance {Id}", attendanceId);
            var success = await _service.HardDeleteAttendanceAsync(attendanceId);
            if (!success) return NotFound();
            await _service.CommitAsync();
            return NoContent();
        }

        /// <summary>Restore a soft-deleted attendance record.</summary>
        [HttpPost("{attendanceId}/restore")]
        public async Task<IActionResult> RestoreAttendance(Guid attendanceId)
        {
            _logger.LogInformation("RestoreAttendance {Id}", attendanceId);
            await _service.RestoreAttendanceAsync(attendanceId);
            await _service.CommitAsync();
            return NoContent();
        }

    }
}
