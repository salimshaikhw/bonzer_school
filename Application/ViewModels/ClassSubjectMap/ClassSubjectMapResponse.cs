namespace Application.ViewModels.ClassSubjectMap;

public class ClassSubjectMapResponse
{
    public Guid Id { get; set; }
    public Guid? ClassId { get; set; }
    public Guid? DivisionId { get; set; }
    public Guid? SubjectId { get; set; }
    public string? AcademicYear { get; set; }
}
