namespace Application.ViewModels.Attendance;

public class ClassAttendanceHeatmapDto
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public List<DateOnly> Dates { get; set; } = new();
    public List<HeatmapStudentInfoDto> Students { get; set; } = new();
    /// <summary>Rows: Student index. Columns: Date index. Value: Status (Present, Absent, Leave, HalfDay, Late, N/A)</summary>
    public List<List<string>> StatusGrid { get; set; } = new();
}

public class HeatmapStudentInfoDto
{
    public Guid StudentId { get; set; }
    public string? StudentName { get; set; }
    public string? RollNumber { get; set; }
}
