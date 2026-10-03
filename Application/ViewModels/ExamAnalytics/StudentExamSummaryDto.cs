using System;
using System.Collections.Generic;

namespace Application.ViewModels.ExamAnalytics
{
    public class StudentExamSummaryDto
    {
        public Guid StudentId { get; set; }
        public Guid ExamId { get; set; }
        public string? ExamName { get; set; }
        public decimal? TotalMarks { get; set; }
        public decimal? MaxTotalMarks { get; set; }
        public decimal? Percentage { get; set; }
        public string? Grade { get; set; }
        public string? Result { get; set; }
        public int? Rank { get; set; }
        public List<StudentExamSubjectLineDto> Subjects { get; set; } = new();
    }

    public class StudentExamSubjectLineDto
    {
        public Guid SubjectId { get; set; }
        public string? SubjectName { get; set; }
        public decimal? MarksObtained { get; set; }
        public decimal? MaxMarks { get; set; }
        public decimal? Percentage { get; set; }
    }
}

