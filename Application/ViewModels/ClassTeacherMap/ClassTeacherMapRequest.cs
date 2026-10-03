namespace Application.ViewModels.ClassTeacherMap;

public class ClassTeacherMapRequest
{

    public Guid? ClassId { get; set; }

    public Guid? DivisionId { get; set; }

    public Guid? SubjectId { get; set; }
    public Guid? StaffId { get; set; }
    public string? AcademicYear { get; set; }
}
