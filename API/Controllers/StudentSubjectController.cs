using Application.Abstractions.Services;
using Application.ViewModels.StudentSubject;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StudentSubjectController : ControllerBase
    {
        private readonly IStudentSubjectService _studentSubjectService;
        private readonly ILogger<StudentSubjectController> _logger;
        public StudentSubjectController(IStudentSubjectService studentSubjectService, ILogger<StudentSubjectController> logger)
        {
            _studentSubjectService = studentSubjectService;
            _logger = logger;
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _studentSubjectService.GetAllAsync();
            return Ok(result);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _studentSubjectService.GetByIdAsync(id);
            return Ok(result);
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StudentSubjectRequest request)
        {
            var result = await _studentSubjectService.CreateAsync(request);
            return Ok(result);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] StudentSubjectRequest request)
        {
            var result = await _studentSubjectService.UpdateAsync(id, request);
            return Ok(result);
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _studentSubjectService.DeleteAsync(id);
            return Ok(result);
        }

    }
}