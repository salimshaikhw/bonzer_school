using Domain.DataAnnotations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Academics;

[Table("classes")]
public partial class Class : AuditableEntity
{

    [References(typeof(Branch))]
    public Guid? BranchId { get; set; }

    [MaxLength(50)]
    public string? Name { get; set; }
}
