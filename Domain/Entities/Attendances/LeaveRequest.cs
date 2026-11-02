using Domain.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Attendances;

[Table("leave_requests")]
[Index(nameof(StudentId))]
[Index(nameof(Status))]
public partial class LeaveRequest : AuditableEntity
{
    [References(typeof(Student))]
    public Guid StudentId { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    [MaxLength(100)]
    public string? LeaveType { get; set; }
    public string? Reason { get; set; }
    [MaxLength(20)]
    [Required]
    public string Status { get; set; } = "Pending";
    public DateTime? RequestedAt { get; set; }
    public Guid? RequestedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedBy { get; set; }
    public string? RejectionReason { get; set; }
}
