namespace Application.ViewModels.Attendance;

public class MonthAttendanceComparisonDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthLabel { get; set; } = null!; // e.g. "Jan 2025"
    public decimal Percentage { get; set; }
    public int PresentCount { get; set; }
    public int TotalDays { get; set; }
}
