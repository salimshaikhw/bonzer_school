using Domain.DataAnnotations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Academics;

[Table("divisions")]
public partial class Division : AuditableEntity
{

    [References(typeof(Class))]
    public Guid? ClassId { get; set; }

    [References(typeof(Branch))]
    public Guid? BranchId { get; set; }

    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(50)]
    public string? Code { get; set; }

    public int? AvailableSeats { get; set; }
}
