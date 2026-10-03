using System;

namespace Application.ViewModels.Attendance;

public class AttendanceCorrectionRequestDto
{
    public Guid Id { get; set; }
    public Guid AttendanceId { get; set; }
    public string Reason { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime? RequestedAt { get; set; }
    public Guid? RequestedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedBy { get; set; }
    public string? RejectionReason { get; set; }
}
