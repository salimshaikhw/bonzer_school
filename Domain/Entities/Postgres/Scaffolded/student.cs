using System;
using System.Collections.Generic;



namespace Domain.Entities.Postgres.Scaffolded
{
    public partial class Student
    {
        public Guid Id { get; set; }

        public Guid? BranchId { get; set; }

        public Guid? AcademicYearId { get; set; }

        public string? AdmissionNo { get; set; }

        public DateOnly? AdmissionDate { get; set; }

        public string? AparId { get; set; }

        public string? PhotoUrl { get; set; }

        public string? SignUrl { get; set; }

        public string? FirstName { get; set; }

        public string? MiddleName { get; set; }

        public string? LastName { get; set; }

        public string? Gender { get; set; }

        public DateOnly? Dob { get; set; }

        public string? BirthPlace { get; set; }

        public string? BirthCertificateNo { get; set; }

        public DateOnly? BirthCertificateIssueDate { get; set; }

        public string? ChildAadhaar { get; set; }

        public string? Religion { get; set; }

        public string? Caste { get; set; }

        public string? Category { get; set; }

        public string? Nationality { get; set; }

        public string? BloodGroup { get; set; }

        public string? MarkIdentification { get; set; }

        public decimal? HeightCm { get; set; }

        public decimal? WeightKg { get; set; }

        public string? MedicalConditions { get; set; }

        public string? Allergies { get; set; }

        public string? Disability { get; set; }

        public string? PreviousSchool { get; set; }

        public string? PreviousClass { get; set; }

        public string? PreviousExam { get; set; }

        public string? PreviousExamResult { get; set; }

        public decimal? PreviousPercentage { get; set; }

        public string? PreviousSchoolContactNumber { get; set; }

        public string? PreviousExamBord { get; set; }

        public string? AddressLine1 { get; set; }

        public string? AddressLine2 { get; set; }

        public string? City { get; set; }

        public string? State { get; set; }

        public string? Pincode { get; set; }

        public string? Phone { get; set; }

        public string? Email { get; set; }

        public string? FatherName { get; set; }

        public string? FatherPhone { get; set; }

        public string? FatherAadhaar { get; set; }

        public string? FatherOccupation { get; set; }

        public string? MotherName { get; set; }

        public string? MotherPhone { get; set; }

        public string? MotherAadhaar { get; set; }

        public string? MotherOccupation { get; set; }

        public string? GuardianName { get; set; }

        public string? GuardianPhone { get; set; }

        public decimal? FamilyIncome { get; set; }

        public Guid? ClassId { get; set; }

        public Guid? DivisionId { get; set; }

        public bool? IsActive { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public DateTime? CreatedAt { get; set; }

        public string? RollNumber { get; set; }

        public string? StudentStatus { get; set; }

        public string? EnrollmentNo { get; set; }

        public bool? IsAadhaarVerified { get; set; }

        public Guid? PromotedToClassId { get; set; }

        public DateOnly? LeavingDate { get; set; }

        public bool? IsTcIssued { get; set; }

        public string? LastExamName { get; set; }

        public string? LastExamResult { get; set; }

        // Scholarship
        public bool? HasScholarship { get; set; }
        public string? ScholarshipType { get; set; }
        public string? ScholarshipStatus { get; set; }
        public decimal? ScholarshipAmount { get; set; }
        public bool? ScholarshipEligible { get; set; }

        // RTE (Right to Education)
        public bool? IsRteSeat { get; set; }
        public bool? RteEligible { get; set; }
        public string? RteApplicationNo { get; set; }
        public string? EwsDgCategory { get; set; }

        // Welfare & reporting
        public bool? IsMinority { get; set; }
        public bool? IsBpl { get; set; }
        public string? BplCardNo { get; set; }
        public string? UdiseCode { get; set; }
        public bool? SingleGirlChild { get; set; }
        public bool? FirstGraduate { get; set; }
    }
}
