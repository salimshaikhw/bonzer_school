using System;

namespace Application.ViewModels.StudentSubject
{
    public class StudentSubjectRequest
    {
        public Guid? StudentId { get; set; }
        public Guid? SubjectId { get; set; }
        public string? AcademicYear { get; set; }
    }
    public class StudentSubjectResponse
    {
        public Guid Id { get; set; }
        public Guid? StudentId { get; set; }
        public Guid? SubjectId { get; set; }
        public string? AcademicYear { get; set; }
    }
}
