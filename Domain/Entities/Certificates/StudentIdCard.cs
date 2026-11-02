using Domain.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Certificates;

[Table("student_id_cards")]
[Index(nameof(CardNumber), IsUnique = true)]
[Index(nameof(StudentId), nameof(AcademicYearId), IsUnique = true)]
[Index(nameof(AcademicYearId), nameof(BranchId), nameof(Sequence), IsUnique = true)]
[Index(nameof(BranchId), nameof(AcademicYearId))]
[Index(nameof(StudentId))]
public partial class StudentIdCard : AuditableEntity
{
    [References(typeof(Student))]
    public Guid StudentId { get; set; }
    [References(typeof(AcademicYear))]
    public Guid AcademicYearId { get; set; }
    [References(typeof(Class))]
    public Guid ClassId { get; set; }
    [References(typeof(Division))]
    public Guid? DivisionId { get; set; }
    [References(typeof(Branch))]
    public Guid BranchId { get; set; }
    [MaxLength(120)]
    [Required]
    public string CardNumber { get; set; } = null!;
    public int Sequence { get; set; }
    [MaxLength(200)]
    public string? StudentNameSnapshot { get; set; }
    [MaxLength(100)]
    public string? AdmissionNoSnapshot { get; set; }
    [MaxLength(100)]
    public string? RollNumberSnapshot { get; set; }
    [MaxLength(100)]
    public string? ClassNameSnapshot { get; set; }
    [MaxLength(100)]
    public string? DivisionNameSnapshot { get; set; }
    public DateOnly? DobSnapshot { get; set; }
    [MaxLength(10)]
    public string? BloodGroupSnapshot { get; set; }
    [MaxLength(200)]
    public string? ParentNameSnapshot { get; set; }
    public string? AddressSnapshot { get; set; }
    public string? PhotoUrlSnapshot { get; set; }
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    /// <summary>GENERATED / PRINTED ...</summary>
    [MaxLength(30)]
    [DefaultValue("GENERATED")]
    public string Status { get; set; } = null!;
    public DateTime? PrintedAt { get; set; }
    public Guid? PrintedBy { get; set; }
    [DefaultValue(false)]
    public bool? IsReprint { get; set; }
    [DefaultValueSql("CURRENT_TIMESTAMP")]
    public DateTime GeneratedAt { get; set; }
    public Guid? GeneratedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
}
