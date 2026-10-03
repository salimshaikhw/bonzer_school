using System;

namespace Application.ViewModels.Certificates;

public class TcCertificateRequest
{
    public Guid StudentId { get; set; }
    public Guid? AcademicYearId { get; set; }
    public Guid? ClassId { get; set; }
    public Guid? DivisionId { get; set; }
    public DateOnly? IssueDate { get; set; }
    public string? Reason { get; set; }
    public string? Remarks { get; set; }
    public bool IsDuplicate { get; set; }
    public Guid? IssuedByStaffId { get; set; }
    public string? Conduct { get; set; }
    public bool? FeesCleared { get; set; }
}
