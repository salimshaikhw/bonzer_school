using System;

namespace Domain.Entities.Postgres.Scaffolded;

public partial class LeaveRequest
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public string? LeaveType { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? RequestedAt { get; set; }
    public Guid? RequestedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedBy { get; set; }
    public string? RejectionReason { get; set; }
}
