using Domain.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Certificates;

[Table("bonafide_certificates")]
[Index(nameof(BonafideNumber), IsUnique = true)]
[Index(nameof(AcademicYearId), nameof(ClassId))]
public partial class BonafideCertificate : AuditableEntity
{
    [References(typeof(Student))]
    public Guid StudentId { get; set; }
    [References(typeof(AcademicYear))]
    public Guid? AcademicYearId { get; set; }
    [References(typeof(Class))]
    public Guid? ClassId { get; set; }
    [References(typeof(Division))]
    public Guid? DivisionId { get; set; }
    [References(typeof(Branch))]
    public Guid? BranchId { get; set; }
    [MaxLength(100)]
    [Required]
    public string BonafideNumber { get; set; } = null!;
    public int Sequence { get; set; }
    public DateOnly IssueDate { get; set; }
    [MaxLength(200)]
    public string? Purpose { get; set; }
    public string? Remarks { get; set; }
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    [DefaultValue(false)]
    public bool IsDuplicate { get; set; }
}
