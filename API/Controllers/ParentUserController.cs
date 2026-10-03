using Application.Abstractions.Services;
using Application.ViewModels.ParentUser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ParentUserController : ControllerBase
    {
        private readonly IParentUserService _parentUserService;
        private readonly ILogger<ParentUserController> _logger;
        public ParentUserController(IParentUserService parentUserService, ILogger<ParentUserController> logger)
        {
            _parentUserService = parentUserService;
            _logger = logger;
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _parentUserService.GetAllAsync();
            return Ok(result);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _parentUserService.GetByIdAsync(id);
            return Ok(result);
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ParentUserRequest request)
        {
            var result = await _parentUserService.CreateAsync(request);
            return Ok(result);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] ParentUserRequest request)
        {
            var result = await _parentUserService.UpdateAsync(id, request);
            return Ok(result);
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _parentUserService.DeleteAsync(id);
            return Ok(result);
        }

    }
}