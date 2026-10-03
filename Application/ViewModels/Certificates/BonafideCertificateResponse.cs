using System;

namespace Application.ViewModels.Certificates;

public class BonafideCertificateResponse
{
    public Guid Id { get; set; }
    public string BonafideNumber { get; set; } = null!;
    public int Sequence { get; set; }
    public DateOnly IssueDate { get; set; }
    public string? Purpose { get; set; }
    public string? Remarks { get; set; }
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public bool IsDuplicate { get; set; }
    public DateTime? CreatedAt { get; set; }

    public Guid StudentId { get; set; }
    public string? StudentName { get; set; }
    public string? AdmissionNo { get; set; }
    public Guid? AcademicYearId { get; set; }
    public string? AcademicYearName { get; set; }
    public Guid? ClassId { get; set; }
    public string? ClassName { get; set; }
    public Guid? DivisionId { get; set; }
    public string? DivisionName { get; set; }
}
