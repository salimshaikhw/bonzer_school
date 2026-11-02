using Domain.DataAnnotations;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Exams;

[Table("exams")]
public partial class Exam : AuditableEntity
{

    [MaxLength(100)]
    public string? Name { get; set; }

    [References(typeof(ExamType))]
    public Guid? ExamTypeId { get; set; }

    [References(typeof(AcademicYear))]
    public Guid? AcademicYearId { get; set; }

    [References(typeof(Branch))]
    public Guid? BranchId { get; set; }

    [References(typeof(Class))]
    public Guid? ClassId { get; set; }

    [References(typeof(Division))]
    public Guid? DivisionId { get; set; }

    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    [MaxLength(20)]
    public string? Status { get; set; }

    [DefaultValue(false)]
    public bool? IsLocked { get; set; }
}
