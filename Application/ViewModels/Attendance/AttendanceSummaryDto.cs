namespace Application.ViewModels.Attendance;

public class AttendanceSummaryDto
{
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int LeaveCount { get; set; }
    public int HalfDayCount { get; set; }
    public int LateCount { get; set; }
    public int TotalDays { get; set; }
    public decimal Percentage { get; set; }
}
