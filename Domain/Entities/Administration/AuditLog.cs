using Domain.DataAnnotations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Administration;

[Table("audit_logs")]
public partial class AuditLog : BaseEntity
{
    public Guid? BranchId { get; set; }

    public Guid? UserId { get; set; }

    [MaxLength(50)]
    public string? Module { get; set; }

    [MaxLength(50)]
    public string? ActionType { get; set; }

    public Guid? RecordId { get; set; }

    public string? Description { get; set; }

    [DefaultValueSql("CURRENT_TIMESTAMP")]
    public DateTime? CreatedAt { get; set; }
}

