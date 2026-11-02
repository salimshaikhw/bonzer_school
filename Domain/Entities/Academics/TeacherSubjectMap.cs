using Domain.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Academics;

[Table("teacher_subject_map")]
[Index(nameof(StaffId), nameof(SubjectId), nameof(ClassId), nameof(DivisionId), nameof(AcademicYear), IsUnique = true)]
public partial class TeacherSubjectMap : AuditableEntity
{
    [References(typeof(Staff))]
    public Guid? StaffId { get; set; }

    [References(typeof(Subject))]
    public Guid? SubjectId { get; set; }

    [References(typeof(Class))]
    public Guid? ClassId { get; set; }

    [References(typeof(Division))]
    public Guid? DivisionId { get; set; }

    [MaxLength(20)]
    public string? AcademicYear { get; set; }
}

