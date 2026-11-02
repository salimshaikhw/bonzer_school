using Domain.DataAnnotations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Communication;

[Table("notifications")]
public partial class Notification : AuditableEntity
{

    [References(typeof(Student))]
    public Guid? StudentId { get; set; }

    [References(typeof(Branch))]
    public Guid? BranchId { get; set; }

    [References(typeof(Division))]
    public Guid? DivisionId { get; set; }

    [MaxLength(50)]
    public string? Type { get; set; }

    [MaxLength(200)]
    public string? Title { get; set; }

    public string? Message { get; set; }

    [DefaultValueSql("CURRENT_TIMESTAMP")]
    public DateTime? SentAt { get; set; }
}
