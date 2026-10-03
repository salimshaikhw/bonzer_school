using System;

namespace Domain.Entities.Postgres.Scaffolded;

public partial class AttendanceCorrectionRequest
{
    public Guid Id { get; set; }
    public Guid AttendanceId { get; set; }
    public string Reason { get; set; } = null!;
    public string Status { get; set; } = "Pending";
    public DateTime? RequestedAt { get; set; }
    public Guid? RequestedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedBy { get; set; }
    public string? RejectionReason { get; set; }
}
