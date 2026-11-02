using Domain.DataAnnotations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Academics;

[Table("class_teacher_map")]
public partial class ClassTeacherMap : AuditableEntity
{

    [References(typeof(Class))]
    public Guid? ClassId { get; set; }

    [References(typeof(Division))]
    public Guid? DivisionId { get; set; }
    [References(typeof(Subject))]
    public Guid? SubjectId { get; set; }
    [References(typeof(Staff))]
    public Guid? StaffId { get; set; }
    [MaxLength(20)]
    public string? AcademicYear { get; set; }
}
