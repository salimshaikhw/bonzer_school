using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Exams;

[Table("exam_types")]
[Index(nameof(Name), IsUnique = true)]
public partial class ExamType : AuditableEntity
{

    [MaxLength(50)]
    public string? Name { get; set; }

    public string? Description { get; set; }
}
