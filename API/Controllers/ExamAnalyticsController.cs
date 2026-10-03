using Application.Abstractions.Services;
using Application.ViewModels.ExamAnalytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ExamAnalyticsController : ControllerBase
    {
        private readonly IExamAnalyticsService _service;
        private readonly ILogger<ExamAnalyticsController> _logger;

        public ExamAnalyticsController(IExamAnalyticsService service, ILogger<ExamAnalyticsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet("student/{studentId}/exam/{examId}/summary")]
        public async Task<IActionResult> GetStudentExamSummary(Guid studentId, Guid examId)
        {
            var result = await _service.GetStudentExamSummaryAsync(studentId, examId);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpGet("student/{studentId}/exams")]
        public async Task<IActionResult> GetStudentExamTrend(Guid studentId, [FromQuery] ExamFilterDto filter)
        {
            var result = await _service.GetStudentExamTrendAsync(studentId, filter);
            return Ok(result);
        }

        [HttpGet("student/{studentId}/subject-growth")]
        public async Task<IActionResult> GetStudentSubjectGrowth(Guid studentId, [FromQuery] ExamFilterDto filter)
        {
            var result = await _service.GetStudentSubjectGrowthAsync(studentId, filter);
            return Ok(result);
        }

        [HttpGet("exam/{examId}/class/{classId}/division/{divisionId}/distribution")]
        public async Task<IActionResult> GetClassExamDistribution(Guid examId, Guid classId, Guid divisionId)
        {
            var result = await _service.GetClassExamDistributionAsync(examId, classId, divisionId);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpGet("exam/{examId}/subject/{subjectId}/students")]
        public async Task<IActionResult> GetSubjectStudentComparison(Guid examId, Guid subjectId)
        {
            var result = await _service.GetSubjectStudentComparisonAsync(examId, subjectId);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpGet("teacher/{teacherId}/exams")]
        public async Task<IActionResult> GetTeacherExamPerformance(Guid teacherId, [FromQuery] TeacherExamFilterDto filter)
        {
            var result = await _service.GetTeacherExamPerformanceAsync(teacherId, filter);
            return Ok(result);
        }

        [HttpGet("teacher/{teacherId}/subjects")]
        public async Task<IActionResult> GetTeacherSubjectPerformance(Guid teacherId, [FromQuery] TeacherSubjectFilterDto filter)
        {
            var result = await _service.GetTeacherSubjectPerformanceAsync(teacherId, filter);
            return Ok(result);
        }

        [HttpGet("teacher/compare")]
        public async Task<IActionResult> CompareTeachers([FromQuery] TeacherComparisonFilterDto filter)
        {
            var result = await _service.CompareTeachersAsync(filter);
            return Ok(result);
        }
    }
}

