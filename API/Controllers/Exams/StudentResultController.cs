using Application.Common.Constants;
using Application.Abstractions.Services;
using Application.ViewModels.StudentResult;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace API.Controllers.Exams
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StudentResultController : ControllerBase
    {
        private readonly IStudentResultService _studentResultService;
        private readonly ILogger<StudentResultController> _logger;
        public StudentResultController(IStudentResultService studentResultService, ILogger<StudentResultController> logger)
        {
            _studentResultService = studentResultService;
            _logger = logger;
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _studentResultService.GetAllAsync();
            return Ok(result);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _studentResultService.GetByIdAsync(id);
            return Ok(result);
        }
        /// <summary>Calculates every student's result of the exam (total, %, pass/fail, grade, rank) from the entered marks.</summary>
        [HttpPost("generate/{examId}")]
        [Authorize(Roles = RoleGroups.TeachingStaff)]
        public async Task<IActionResult> Generate(Guid examId)
        {
            return Ok(await _studentResultService.GenerateResultsAsync(examId));
        }

        [HttpPost]
        [Authorize(Roles = RoleGroups.TeachingStaff)]
        public async Task<IActionResult> Create([FromBody] StudentResultRequest request)
        {
            var result = await _studentResultService.CreateAsync(request);
            return Ok(result);
        }
        [HttpPut("{id}")]
        [Authorize(Roles = RoleGroups.TeachingStaff)]
        public async Task<IActionResult> Update(Guid id, [FromBody] StudentResultRequest request)
        {
            var result = await _studentResultService.UpdateAsync(id, request);
            return Ok(result);
        }
        [HttpDelete("{id}")]
        [Authorize(Roles = RoleGroups.TeachingStaff)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _studentResultService.DeleteAsync(id);
            return Ok(result);
        }
    }
}