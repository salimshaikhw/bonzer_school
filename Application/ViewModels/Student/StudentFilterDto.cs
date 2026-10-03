using System;

namespace Application.ViewModels.Student
{
    /// <summary>Simple filter for SearchStudents. All properties are optional; provided values are ANDed.</summary>
    public class StudentFilterDto
    {
        public string? Keyword { get; set; }
        public Guid? BranchId { get; set; }
        public Guid? ClassId { get; set; }
        public Guid? DivisionId { get; set; }
        public Guid? AcademicYearId { get; set; }
        public string? StudentStatus { get; set; }
        public string? Gender { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Pincode { get; set; }
    }
}
