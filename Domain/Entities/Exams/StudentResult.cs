using Domain.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Exams;

[Table("student_results")]
[Index(nameof(ExamId), nameof(StudentId), IsUnique = true)]
[Index(nameof(StudentId))]
public partial class StudentResult : AuditableEntity
{

    [References(typeof(Exam))]
    public Guid? ExamId { get; set; }

    [References(typeof(Student))]
    public Guid? StudentId { get; set; }

    [Precision(6, 2)]
    public decimal? TotalMarks { get; set; }

    [Precision(5, 2)]
    public decimal? Percentage { get; set; }

    [MaxLength(5)]
    public string? Grade { get; set; }

    [MaxLength(20)]
    public string? Result { get; set; }

    public int? Rank { get; set; }

    [DefaultValueSql("CURRENT_TIMESTAMP")]
    public DateTime? GeneratedAt { get; set; }
}
