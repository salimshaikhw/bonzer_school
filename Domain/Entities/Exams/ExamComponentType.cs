using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Exams;

[Table("exam_component_types")]
[Index(nameof(Name), IsUnique = true)]
public partial class ExamComponentType : AuditableEntity
{

    [MaxLength(50)]
    [Required]
    public string Name { get; set; } = null!;

    [MaxLength(20)]
    public string? Code { get; set; }
}
