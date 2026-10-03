using Application.Abstractions.Services;
using Application.ViewModels.Timetable;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TimetableController : ControllerBase
    {
        private readonly ITimetableService _timetableService;
        private readonly ILogger<TimetableController> _logger;
        public TimetableController(ITimetableService timetableService, ILogger<TimetableController> logger)
        {
            _timetableService = timetableService;
            _logger = logger;
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _timetableService.GetAllAsync();
            return Ok(result);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _timetableService.GetByIdAsync(id);
            return Ok(result);
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TimetableRequest request)
        {
            var result = await _timetableService.CreateAsync(request);
            return Ok(result);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] TimetableRequest request)
        {
            var result = await _timetableService.UpdateAsync(id, request);
            return Ok(result);
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _timetableService.DeleteAsync(id);
            return Ok(result);
        }
    }
}