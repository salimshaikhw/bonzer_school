using System;

namespace Infrastructure;

public partial class TcCertificate
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid? AcademicYearId { get; set; }
    public Guid? ClassId { get; set; }
    public Guid? DivisionId { get; set; }
    public Guid? BranchId { get; set; }
    public string TcNumber { get; set; } = null!;
    public int Sequence { get; set; }
    public DateOnly IssueDate { get; set; }
    public string? Reason { get; set; }
    public string? Remarks { get; set; }
    public bool IsDuplicate { get; set; }
    public Guid? IssuedByStaffId { get; set; }
    public string? Conduct { get; set; }
    public bool? FeesCleared { get; set; }
    public DateTime? CreatedAt { get; set; }
}
