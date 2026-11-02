using Domain.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Personnel;

[Table("staff")]
[FilteredIndex(nameof(BranchId), nameof(StaffCode), Filter = "staff_code IS NOT NULL", IsUnique = true)]
public partial class Staff : AuditableEntity
{
    [References(typeof(Branch))]
    public Guid? BranchId { get; set; }

    [References(typeof(Department))]
    public Guid? DeptId { get; set; }

    [MaxLength(50)]
    public string? StaffCode { get; set; }

    [MaxLength(100)]
    public string? Designation { get; set; }

    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? MiddleName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    [MaxLength(20)]
    public string? Gender { get; set; }

    public DateOnly? Dob { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(150)]
    public string? Email { get; set; }

    public string? Address { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? State { get; set; }

    public DateOnly? JoiningDate { get; set; }

    [Precision(12, 2)]
    public decimal? Salary { get; set; }

    [MaxLength(50)]
    public string? Qualification { get; set; }

    [MaxLength(200)]
    public string? QualificationDetails { get; set; }

    [MaxLength(200)]
    public string? PreviousEmployer { get; set; }

    public int? YearsOfExperience { get; set; }

    [DefaultValue(true)]
    public bool? IsActive { get; set; }
}

