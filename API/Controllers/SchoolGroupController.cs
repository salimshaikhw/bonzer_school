using Application.Abstractions.Services;
using Application.ViewModels.ClassTeacherMap;
using Application.ViewModels.SchoolGroup;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Require authentication for all endpoints
    public class SchoolGroupController : ControllerBase
    {
        private readonly ISchoolGroupService _service;
        private readonly ILogger<SchoolGroupController> _logger;

        public SchoolGroupController(ISchoolGroupService service, ILogger<SchoolGroupController> logger )
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
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateSchoolGroupInput input)
        {
            var result = await _service.CreateAsync(input);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }


        //[HttpPut("{id}")]
        //[Authorize(Roles = "Admin")]
        //public async Task<IActionResult> Update(Guid id , [FromBody] UpdateSchoolGroupInput input)
        //{
        //    var result = await _service.UpdateAsync(input);
        //    return Ok(result);
        //}


        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSchoolGroupInput request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _service.UpdateAsync(id, request);
            if (result == null)
                return NotFound($"School group with ID {id} not found.");

            return Ok(result);
        }


        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
    }
}
