using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Exams;

[Table("grade_master")]
public partial class GradeMaster : AuditableEntity
{

    [MaxLength(5)]
    public string? Grade { get; set; }

    [Precision(5, 2)]
    public decimal? MinPercent { get; set; }

    [Precision(5, 2)]
    public decimal? MaxPercent { get; set; }
}
