using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Fees;

[Table("fee_heads")]
public partial class FeeHead : AuditableEntity
{

    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(50)]
    public string? AccountCode { get; set; }
}
