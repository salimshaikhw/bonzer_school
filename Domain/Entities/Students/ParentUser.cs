using Domain.DataAnnotations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Students;

[Table("parent_users")]
public partial class ParentUser : AuditableEntity
{

    [References(typeof(Student))]
    public Guid? StudentId { get; set; }

    [MaxLength(100)]
    public string? Username { get; set; }

    public string? PasswordHash { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }
}
