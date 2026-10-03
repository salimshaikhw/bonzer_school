using Application.Abstractions.Services;
using Application.ViewModels.TeacherSubjectMap;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TeacherSubjectMapController : ControllerBase
    {
        private readonly ITeacherSubjectMapService _teacherSubjectMapService;
        private readonly ILogger<TeacherSubjectMapController> _logger;
        public TeacherSubjectMapController(ITeacherSubjectMapService teacherSubjectMapService, ILogger<TeacherSubjectMapController> logger)
        {
            _teacherSubjectMapService = teacherSubjectMapService;
            _logger = logger;
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _teacherSubjectMapService.GetAllAsync();
            return Ok(result);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _teacherSubjectMapService.GetByIdAsync(id);
            return Ok(result);
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TeacherSubjectMapRequest request)
        {
            var result = await _teacherSubjectMapService.CreateAsync(request);
            return Ok(result);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] TeacherSubjectMapRequest request)
        {
            var result = await _teacherSubjectMapService.UpdateAsync(id, request);
            return Ok(result);
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _teacherSubjectMapService.DeleteAsync(id);
            return Ok(result);
        }
    }
}