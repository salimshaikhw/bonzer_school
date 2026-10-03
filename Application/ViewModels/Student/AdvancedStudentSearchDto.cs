using System;

namespace Application.ViewModels.Student
{
    /// <summary>Multi-criteria search for AdvancedStudentSearch. All properties optional; provided values are ANDed.</summary>
    public class AdvancedStudentSearchDto
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
        public DateOnly? DobFrom { get; set; }
        public DateOnly? DobTo { get; set; }
        public DateOnly? AdmissionDateFrom { get; set; }
        public DateOnly? AdmissionDateTo { get; set; }
        public DateOnly? LeavingDateFrom { get; set; }
        public DateOnly? LeavingDateTo { get; set; }
        public decimal? HeightMin { get; set; }
        public decimal? HeightMax { get; set; }
        public decimal? WeightMin { get; set; }
        public decimal? WeightMax { get; set; }
        public decimal? FamilyIncomeMin { get; set; }
        public decimal? FamilyIncomeMax { get; set; }
        public string? Religion { get; set; }
        public string? Caste { get; set; }
        public string? Category { get; set; }
        public string? BloodGroup { get; set; }
        public string? Disability { get; set; }
        public string? PreviousSchool { get; set; }
        public string? PreviousBoard { get; set; }
    }
}
