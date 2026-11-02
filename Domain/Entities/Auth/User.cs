using Domain.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Auth;

[Table("users")]
[Index(nameof(Username), IsUnique = true)]
public partial class User : AuditableEntity
{


    [References(typeof(Branch))]
    public Guid? BranchId { get; set; }

    [References(typeof(Role))]
    public Guid? RoleId { get; set; }

    [References(typeof(Staff))]
    public Guid? StaffId { get; set; }

    [MaxLength(100)]
    public string? Username { get; set; }

    [MaxLength(150)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    public string? PasswordHash { get; set; }

    [DefaultValue(true)]
    public bool? IsActive { get; set; }

    public DateTime? LastLogin { get; set; }

    [MaxLength(150)]
    public string? SecurityQuestion { get; set; }

    [MaxLength(50)]
    public string? SecurityAnswer { get; set; }
}
