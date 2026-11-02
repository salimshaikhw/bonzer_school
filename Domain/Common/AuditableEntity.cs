using System.ComponentModel.DataAnnotations;
using Domain.DataAnnotations;

namespace Domain.Common;

/// <summary>
/// Who created / last changed a row, and when. Filled in automatically on SaveChanges by
/// Infrastructure's AuditableEntityInterceptor; services do not need to set these.
/// CreatedAt/CreatedBy are never overwritten by an update.
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    [DefaultValueSql("CURRENT_TIMESTAMP")]
    public DateTime CreatedAt { get; set; }

    /// <summary>ASP.NET Identity user id of the creator; null for system/background writes.</summary>
    [MaxLength(450)]
    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    /// <summary>ASP.NET Identity user id of the last editor.</summary>
    [MaxLength(450)]
    public string? UpdatedBy { get; set; }
}
