using Domain.DataAnnotations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Students;

[Table("student_subjects")]
public partial class StudentSubject : AuditableEntity
{

    [References(typeof(Student))]
    public Guid? StudentId { get; set; }

    [References(typeof(Subject))]
    public Guid? SubjectId { get; set; }

    [MaxLength(20)]
    public string? AcademicYear { get; set; }
}
