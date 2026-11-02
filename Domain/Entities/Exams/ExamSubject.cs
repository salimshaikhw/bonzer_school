using Domain.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Exams;

[Table("exam_subjects")]
[Index(nameof(ExamId))]
public partial class ExamSubject : AuditableEntity
{

    [References(typeof(Exam))]
    public Guid? ExamId { get; set; }

    [References(typeof(Subject))]
    public Guid? SubjectId { get; set; }

    [Precision(6, 2)]
    public decimal? TotalMaxMarks { get; set; }

    [Precision(6, 2)]
    public decimal? TotalPassMarks { get; set; }

    public DateOnly? ExamDate { get; set; }

    [Column(TypeName = "time without time zone")]
    public TimeOnly? StartTime { get; set; }

    [Column(TypeName = "time without time zone")]
    public TimeOnly? EndTime { get; set; }

    public int? DurationMinutes { get; set; }
}
