using System;

namespace Application.ViewModels.Attendance;

public class LeaveRequestDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string? StudentName { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public string? LeaveType { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = null!;
    public DateTime? RequestedAt { get; set; }
    public Guid? RequestedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedBy { get; set; }
    public string? RejectionReason { get; set; }
}

public class ApplyLeaveDto
{
    public Guid StudentId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string LeaveType { get; set; } = null!;
    public string? Reason { get; set; }
}
