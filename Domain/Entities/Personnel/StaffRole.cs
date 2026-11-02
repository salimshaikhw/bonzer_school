using Domain.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Personnel;

[Table("staff_roles")]
public partial class StaffRole : AuditableEntity
{

    [References(typeof(Staff))]
    public Guid? StaffId { get; set; }

    [References(typeof(Role))]
    public Guid? RoleId { get; set; }
}
