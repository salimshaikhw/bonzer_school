using System;

namespace Application.ViewModels.TeacherSubjectMap
{
    public class TeacherSubjectMapRequest
    {
        public Guid? StaffId { get; set; }
        public Guid? SubjectId { get; set; }
        public Guid? ClassId { get; set; }
        public Guid? DivisionId { get; set; }
        public string? AcademicYear { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
    public class TeacherSubjectMapResponse
    {
        public Guid Id { get; set; }
        public Guid? StaffId { get; set; }
        public Guid? SubjectId { get; set; }
        public Guid? ClassId { get; set; }
        public Guid? DivisionId { get; set; }
        public string? AcademicYear { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
