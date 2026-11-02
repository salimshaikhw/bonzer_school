using Application.Common.Constants;
using Application.Abstractions.Services;
using Application.ViewModels.FeeInstallment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace API.Controllers.Fees
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FeeInstallmentController : ControllerBase
    {
        private readonly IFeeInstallmentService _service;
        private readonly ILogger<FeeInstallmentController> _logger;

        public FeeInstallmentController(IFeeInstallmentService service, ILogger<FeeInstallmentController> logger)
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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = RoleGroups.Finance)]
        public async Task<IActionResult> Create([FromBody] FeeInstallmentRequest request)
        {
            var result = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = RoleGroups.Finance)]
        public async Task<IActionResult> Update(Guid id, [FromBody] FeeInstallmentRequest request)
        {
            var result = await _service.UpdateAsync(id, request);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = RoleGroups.Finance)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }
    }
}