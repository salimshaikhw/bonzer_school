using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Organization;

[Table("school_groups")]
public partial class SchoolGroup : AuditableEntity
{

    [MaxLength(200)]
    public string? Name { get; set; }

    [MaxLength(50)]
    public string? Code { get; set; }

    public string? Address { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(150)]
    public string? Email { get; set; }
}
