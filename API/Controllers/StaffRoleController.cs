using Application.Abstractions.Services;
using Application.ViewModels.StaffRole;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StaffRoleController : ControllerBase
    {
        private readonly IStaffRoleService _staffRoleService;
        private readonly ILogger<StaffRoleController> _logger;
        public StaffRoleController(IStaffRoleService staffRoleService, ILogger<StaffRoleController> logger)
        {
            _staffRoleService = staffRoleService;
            _logger = logger;
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _staffRoleService.GetAllAsync();
            return Ok(result);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _staffRoleService.GetByIdAsync(id);
            return Ok(result);
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StaffRoleRequest request)
        {
            var result = await _staffRoleService.CreateAsync(request);
            return Ok(result);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] StaffRoleRequest request)
        {
            var result = await _staffRoleService.UpdateAsync(id, request);
            return Ok(result);
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _staffRoleService.DeleteAsync(id);
            return Ok(result);
        }

    }
}