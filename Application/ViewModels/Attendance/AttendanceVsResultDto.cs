namespace Application.ViewModels.Attendance;

public class AttendanceVsResultDto
{
    public decimal ClassAttendancePercentage { get; set; }
    public decimal? ClassAverageResultPercentage { get; set; }
    public string? CorrelationNote { get; set; }
    public List<StudentAttendanceVsResultDto> PerStudent { get; set; } = new();
}

public class StudentAttendanceVsResultDto
{
    public Guid StudentId { get; set; }
    public string? StudentName { get; set; }
    public decimal AttendancePercentage { get; set; }
    public decimal? ResultPercentage { get; set; }
    public string? Grade { get; set; }
}
