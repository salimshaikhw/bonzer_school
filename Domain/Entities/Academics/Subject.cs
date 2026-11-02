using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Academics;

[Table("subjects")]
public partial class Subject : AuditableEntity
{

    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(50)]
    public string? Code { get; set; }
}
