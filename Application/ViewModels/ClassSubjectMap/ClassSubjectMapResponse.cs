namespace Application.ViewModels.ClassSubjectMap;

public class ClassSubjectMapResponse : AuditableResponse
{
    public Guid? ClassId { get; set; }
    public Guid? DivisionId { get; set; }
    public Guid? SubjectId { get; set; }
    public string? AcademicYear { get; set; }
}
