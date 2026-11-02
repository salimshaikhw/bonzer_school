using Domain.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Exams;

[Table("marks")]
[Index(nameof(StudentId), nameof(ExamSubjectComponentId), IsUnique = true)]
[Index(nameof(ExamSubjectComponentId))]
public partial class Mark : AuditableEntity
{

    [References(typeof(ExamSubjectComponent))]
    public Guid? ExamSubjectComponentId { get; set; }

    [References(typeof(Student))]
    public Guid? StudentId { get; set; }

    [Precision(6, 2)]
    public decimal? MarksObtained { get; set; }

    [DefaultValue(false)]
    public bool? IsAbsent { get; set; }

    [DefaultValue(false)]
    public bool? IsMedical { get; set; }

    [DefaultValue(false)]
    public bool? IsExempt { get; set; }

    public string? Remark { get; set; }

    public Guid? EnteredBy { get; set; }

    [DefaultValueSql("CURRENT_TIMESTAMP")]
    public DateTime? EnteredAt { get; set; }
}
