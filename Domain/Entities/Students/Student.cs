using Domain.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Students;

[Table("students")]
[Index(nameof(BranchId), nameof(ClassId))]
[Index(nameof(DivisionId))]
[Index(nameof(RollNumber))]
[FilteredIndex(nameof(BranchId), nameof(AdmissionNo), Filter = "admission_no IS NOT NULL", IsUnique = true)]
public partial class Student : AuditableEntity
{
    [References(typeof(Branch))]
    public Guid? BranchId { get; set; }

    [References(typeof(AcademicYear))]
    public Guid? AcademicYearId { get; set; }

    [MaxLength(50)]
    public string? AdmissionNo { get; set; }

    public DateOnly? AdmissionDate { get; set; }

    public string? AparId { get; set; }

    public string? PhotoUrl { get; set; }

    public string? SignUrl { get; set; }

    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? MiddleName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    [MaxLength(20)]
    public string? Gender { get; set; }

    public DateOnly? Dob { get; set; }

    [MaxLength(200)]
    public string? BirthPlace { get; set; }

    [MaxLength(100)]
    public string? BirthCertificateNo { get; set; }

    public DateOnly? BirthCertificateIssueDate { get; set; }

    [MaxLength(20)]
    public string? ChildAadhaar { get; set; }

    [MaxLength(50)]
    public string? Religion { get; set; }

    [MaxLength(50)]
    public string? Caste { get; set; }

    [MaxLength(50)]
    public string? Category { get; set; }

    [MaxLength(50)]
    public string? Nationality { get; set; }

    [MaxLength(10)]
    public string? BloodGroup { get; set; }

    public string? MarkIdentification { get; set; }

    [Precision(5, 2)]
    public decimal? HeightCm { get; set; }

    [Precision(5, 2)]
    public decimal? WeightKg { get; set; }

    public string? MedicalConditions { get; set; }

    public string? Allergies { get; set; }

    public string? Disability { get; set; }

    [MaxLength(200)]
    public string? PreviousSchool { get; set; }

    [MaxLength(50)]
    public string? PreviousClass { get; set; }

    [MaxLength(100)]
    public string? PreviousExam { get; set; }

    [MaxLength(50)]
    public string? PreviousExamResult { get; set; }

    [Precision(5, 2)]
    public decimal? PreviousPercentage { get; set; }

    [MaxLength(200)]
    public string? PreviousSchoolContactNumber { get; set; }

    [MaxLength(100)]
    public string? PreviousExamBoard { get; set; }

    public string? AddressLine1 { get; set; }

    public string? AddressLine2 { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? State { get; set; }

    [MaxLength(20)]
    public string? Pincode { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(150)]
    public string? Email { get; set; }

    [MaxLength(150)]
    public string? FatherName { get; set; }

    [MaxLength(20)]
    public string? FatherPhone { get; set; }

    [MaxLength(20)]
    public string? FatherAadhaar { get; set; }

    [MaxLength(100)]
    public string? FatherOccupation { get; set; }

    [MaxLength(150)]
    public string? MotherName { get; set; }

    [MaxLength(20)]
    public string? MotherPhone { get; set; }

    [MaxLength(20)]
    public string? MotherAadhaar { get; set; }

    [MaxLength(100)]
    public string? MotherOccupation { get; set; }

    [MaxLength(150)]
    public string? GuardianName { get; set; }

    [MaxLength(20)]
    public string? GuardianPhone { get; set; }

    [Precision(12, 2)]
    public decimal? FamilyIncome { get; set; }

    [References(typeof(Class))]
    public Guid? ClassId { get; set; }

    [References(typeof(Division))]
    public Guid? DivisionId { get; set; }

    [DefaultValue(true)]
    public bool? IsActive { get; set; }

    [MaxLength(50)]
    public string? RollNumber { get; set; }

    [MaxLength(50)]
    [DefaultValue("ACTIVE")]
    public string? StudentStatus { get; set; }

    [MaxLength(100)]
    public string? EnrollmentNo { get; set; }

    [DefaultValue(false)]
    public bool? IsAadhaarVerified { get; set; }

    [References(typeof(Class))]
    public Guid? PromotedToClassId { get; set; }

    public DateOnly? LeavingDate { get; set; }

    [DefaultValue(false)]
    public bool? IsTcIssued { get; set; }

    [MaxLength(200)]
    public string? LastExamName { get; set; }

    [MaxLength(100)]
    public string? LastExamResult { get; set; }

    // Scholarship

    [DefaultValue(false)]
    public bool? HasScholarship { get; set; }

    [MaxLength(100)]
    public string? ScholarshipType { get; set; }

    [MaxLength(50)]
    public string? ScholarshipStatus { get; set; }

    [Precision(12, 2)]
    public decimal? ScholarshipAmount { get; set; }

    [DefaultValue(false)]
    public bool? ScholarshipEligible { get; set; }

    // RTE (Right to Education)

    [DefaultValue(false)]
    public bool? IsRteSeat { get; set; }

    [DefaultValue(false)]
    public bool? RteEligible { get; set; }

    [MaxLength(100)]
    public string? RteApplicationNo { get; set; }

    [MaxLength(20)]
    public string? EwsDgCategory { get; set; }

    // Welfare & reporting

    [DefaultValue(false)]
    public bool? IsMinority { get; set; }

    [DefaultValue(false)]
    public bool? IsBpl { get; set; }

    [MaxLength(100)]
    public string? BplCardNo { get; set; }

    [MaxLength(50)]
    public string? UdiseCode { get; set; }

    [DefaultValue(false)]
    public bool? SingleGirlChild { get; set; }

    [DefaultValue(false)]
    public bool? FirstGraduate { get; set; }
}

