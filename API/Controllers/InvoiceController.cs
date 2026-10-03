using Application.Abstractions.Services;
using Application.Exceptions;
using Application.ViewModels.Invoice;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class InvoiceController : ControllerBase
    {
        private readonly IInvoiceService _service;
        private readonly ILogger<InvoiceController> _logger;

        public InvoiceController(IInvoiceService service, ILogger<InvoiceController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id:guid}/statement")]
        public async Task<IActionResult> GetStatement(Guid id)
        {
            var result = await _service.GetInvoiceStatementAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] InvoiceRequest request)
        {
            var result = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        /// <summary>
        /// Create an open invoice from the student fee dashboard (per fee head remaining) when they have no open invoice for the year.
        /// </summary>
        [HttpPost("from-student-schedule")]
        public async Task<IActionResult> CreateFromStudentSchedule([FromBody] CreateInvoiceFromScheduleRequest request)
        {
            try
            {
                var result = await _service.CreateFromStudentScheduleAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (BaseServiceException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] InvoiceRequest request)
        {
            var result = await _service.UpdateAsync(id, request);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }
    }
}