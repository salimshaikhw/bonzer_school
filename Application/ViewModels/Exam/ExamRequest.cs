namespace Application.ViewModels.Exam;

public class ExamRequest
{
    public string? Name { get; set; }
    public Guid? ExamTypeId { get; set; }
    public Guid? AcademicYearId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? ClassId { get; set; }
    public Guid? DivisionId { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Status { get; set; }
    public bool? IsLocked { get; set; }
    public DateTime? CreatedAt { get; set; }
}
