using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Attendances;

[Table("attendance_correction_requests")]
public partial class AttendanceCorrectionRequest : AuditableEntity
{
    public Guid AttendanceId { get; set; }
    [MaxLength(2000)]
    [Required]
    public string Reason { get; set; } = null!;
    [MaxLength(20)]
    [Required]
    public string Status { get; set; } = "Pending";
    public DateTime? RequestedAt { get; set; }
    public Guid? RequestedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedBy { get; set; }
    public string? RejectionReason { get; set; }
}
