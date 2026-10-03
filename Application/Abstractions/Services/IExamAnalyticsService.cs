using Application.ViewModels.ExamAnalytics;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    /// <summary>
    /// Read-only analytics for exams, results, subjects and teachers.
    /// </summary>
    public interface IExamAnalyticsService
    {
        Task<StudentExamSummaryDto?> GetStudentExamSummaryAsync(Guid studentId, Guid examId);
        Task<IReadOnlyList<StudentExamTrendPointDto>> GetStudentExamTrendAsync(Guid studentId, ExamFilterDto filter);
        Task<StudentSubjectGrowthDto> GetStudentSubjectGrowthAsync(Guid studentId, ExamFilterDto filter);
        Task<ClassExamDistributionDto?> GetClassExamDistributionAsync(Guid examId, Guid classId, Guid divisionId);
        Task<SubjectStudentComparisonDto?> GetSubjectStudentComparisonAsync(Guid examId, Guid subjectId);
        Task<IReadOnlyList<TeacherExamPerformanceDto>> GetTeacherExamPerformanceAsync(Guid teacherId, TeacherExamFilterDto filter);
        Task<IReadOnlyList<TeacherSubjectPerformanceDto>> GetTeacherSubjectPerformanceAsync(Guid teacherId, TeacherSubjectFilterDto filter);
        Task<IReadOnlyList<TeacherComparisonPointDto>> CompareTeachersAsync(TeacherComparisonFilterDto filter);
    }
}

