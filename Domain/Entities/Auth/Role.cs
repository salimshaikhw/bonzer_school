using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Auth;

[Table("roles")]
[Index(nameof(Name), IsUnique = true)]
public partial class Role : AuditableEntity
{

    [MaxLength(50)]
    public string? Name { get; set; }

    public string? Description { get; set; }
}
