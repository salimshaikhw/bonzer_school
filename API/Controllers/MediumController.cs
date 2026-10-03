using Application.Abstractions.Services;
using Application.ViewModels.Medium;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MediumController : ControllerBase
    {
        private readonly IMediumService _mediumService;
        private readonly ILogger<MediumController> _logger;
        public MediumController(IMediumService mediumService, ILogger<MediumController> logger)
        {
            _mediumService = mediumService;
            _logger = logger;
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _mediumService.GetAllAsync();
            return Ok(result);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _mediumService.GetByIdAsync(id);
            return Ok(result);
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] MediumRequest request)
        {
            var result = await _mediumService.CreateAsync(request);
            return Ok(result);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] MediumRequest request)
        {
            var result = await _mediumService.UpdateAsync(id, request);
            return Ok(result);
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _mediumService.DeleteAsync(id);
            return Ok(result);
        }

    }
}