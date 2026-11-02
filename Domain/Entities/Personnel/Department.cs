using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Personnel;

[Table("department")]
public partial class Department : AuditableEntity
{
    [MaxLength(20)]
    public string? Name { get; set; }
}
