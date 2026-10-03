using System;

namespace Application.ViewModels.Certificates;

public class BonafideCertificateRequest
{
    public Guid StudentId { get; set; }
    public Guid? AcademicYearId { get; set; }
    public Guid? ClassId { get; set; }
    public Guid? DivisionId { get; set; }
    public DateOnly? IssueDate { get; set; }
    public string? Purpose { get; set; }
    public string? Remarks { get; set; }
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public bool IsDuplicate { get; set; }
}
