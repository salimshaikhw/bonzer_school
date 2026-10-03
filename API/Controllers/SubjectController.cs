using Application.Abstractions.Services;
using Application.ViewModels.Subject;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SubjectController : ControllerBase
    {
        private readonly ISubjectService _subjectService;
        private readonly ILogger<SubjectController> _logger;
        public SubjectController(ISubjectService subjectService, ILogger<SubjectController> logger)
        {
            _subjectService = subjectService;
            _logger = logger;
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _subjectService.GetAllAsync();
            return Ok(result);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _subjectService.GetByIdAsync(id);
            return Ok(result);
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SubjectRequest request)
        {
            var result = await _subjectService.CreateAsync(request);
            return Ok(result);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] SubjectRequest request)
        {
            var result = await _subjectService.UpdateAsync(id, request);
            return Ok(result);
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _subjectService.DeleteAsync(id);
            return Ok(result);
        }

        
    }
}