using System;
using System.Collections.Generic;

namespace Application.ViewModels.ExamAnalytics
{
    public class ExamFilterDto
    {
        public List<Guid>? ExamIds { get; set; }
        public Guid? AcademicYearId { get; set; }
        public Guid? ExamTypeId { get; set; }
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
    }

    public class TeacherExamFilterDto
    {
        public Guid? AcademicYearId { get; set; }
        public Guid? ClassId { get; set; }
        public Guid? SubjectId { get; set; }
    }

    public class TeacherSubjectFilterDto
    {
        public Guid? AcademicYearId { get; set; }
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
    }

    public class TeacherComparisonFilterDto
    {
        public Guid ExamId { get; set; }
        public Guid? SubjectId { get; set; }
        public Guid? ClassId { get; set; }
        public Guid? DivisionId { get; set; }
    }

    public class StudentExamTrendPointDto
    {
        public Guid ExamId { get; set; }
        public string? ExamName { get; set; }
        public DateOnly? ExamDate { get; set; }
        public decimal? TotalMarks { get; set; }
        public decimal? MaxTotalMarks { get; set; }
        public decimal? Percentage { get; set; }
        public string? Grade { get; set; }
        public string? Result { get; set; }
    }

    public class StudentSubjectGrowthDto
    {
        public Guid StudentId { get; set; }
        public List<StudentSubjectGrowthSeriesDto> Subjects { get; set; } = new();
    }

    public class StudentSubjectGrowthSeriesDto
    {
        public Guid SubjectId { get; set; }
        public string? SubjectName { get; set; }
        public List<StudentSubjectGrowthPointDto> Points { get; set; } = new();
    }

    public class StudentSubjectGrowthPointDto
    {
        public Guid ExamId { get; set; }
        public string? ExamName { get; set; }
        public DateOnly? ExamDate { get; set; }
        public decimal? MarksObtained { get; set; }
        public decimal? MaxMarks { get; set; }
        public decimal? Percentage { get; set; }
    }

    public class ClassExamDistributionDto
    {
        public Guid ExamId { get; set; }
        public Guid ClassId { get; set; }
        public Guid DivisionId { get; set; }
        public int TotalStudents { get; set; }
        public int PassedCount { get; set; }
        public int FailedCount { get; set; }
        public List<GradeBucketDto> GradeBuckets { get; set; } = new();
        public List<SubjectAverageDto> SubjectAverages { get; set; } = new();
    }

    public class GradeBucketDto
    {
        public string Grade { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class SubjectAverageDto
    {
        public Guid SubjectId { get; set; }
        public string? SubjectName { get; set; }
        public decimal? AverageMarks { get; set; }
        public decimal? MaxMarks { get; set; }
        public decimal? AveragePercentage { get; set; }
    }

    public class SubjectStudentComparisonDto
    {
        public Guid ExamId { get; set; }
        public Guid SubjectId { get; set; }
        public string? SubjectName { get; set; }
        public List<SubjectStudentLineDto> Students { get; set; } = new();
    }

    public class SubjectStudentLineDto
    {
        public Guid StudentId { get; set; }
        public string? StudentName { get; set; }
        public string? RollNumber { get; set; }
        public decimal? MarksObtained { get; set; }
        public decimal? MaxMarks { get; set; }
        public decimal? Percentage { get; set; }
        public string? Grade { get; set; }
    }

    public class TeacherExamPerformanceDto
    {
        public Guid TeacherId { get; set; }
        public Guid ExamId { get; set; }
        public string? ExamName { get; set; }
        public decimal? AveragePercentage { get; set; }
        public int StudentCount { get; set; }
        public int PassedCount { get; set; }
        public int FailedCount { get; set; }
    }

    public class TeacherSubjectPerformanceDto
    {
        public Guid TeacherId { get; set; }
        public Guid SubjectId { get; set; }
        public string? SubjectName { get; set; }
        public List<TeacherSubjectPerformancePointDto> Points { get; set; } = new();
    }

    public class TeacherSubjectPerformancePointDto
    {
        public Guid ExamId { get; set; }
        public string? ExamName { get; set; }
        public DateOnly? ExamDate { get; set; }
        public decimal? AverageMarks { get; set; }
        public decimal? MaxMarks { get; set; }
        public decimal? AveragePercentage { get; set; }
    }

    public class TeacherComparisonPointDto
    {
        public Guid TeacherId { get; set; }
        public string? TeacherName { get; set; }
        public decimal? AveragePercentage { get; set; }
        public int StudentCount { get; set; }
        public int PassedCount { get; set; }
        public int FailedCount { get; set; }
    }
}

