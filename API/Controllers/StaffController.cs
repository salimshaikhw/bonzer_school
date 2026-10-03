using Application.Abstractions.Services;
using Application.ViewModels.Staff;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StaffController : ControllerBase
    {
        private readonly IStaffService _staffService;
        private readonly ILogger<StaffController> _logger;
        public StaffController(IStaffService staffService, ILogger<StaffController> logger)
        {
            _staffService = staffService;
            _logger = logger;
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _staffService.GetAllAsync();
            return Ok(result);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _staffService.GetByIdAsync(id);
            return Ok(result);
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StaffRequest request)
        {
            var result = await _staffService.CreateAsync(request);
            return Ok(result);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] StaffRequest request)
        {
            var result = await _staffService.UpdateAsync(id, request);
            return Ok(result);
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _staffService.DeleteAsync(id);
            return Ok(result);
        }

    }
}