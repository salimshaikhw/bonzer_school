namespace Application.ViewModels.Attendance;

public class ClassStudentAttendanceSummaryDto
{
    public Guid StudentId { get; set; }
    public string? StudentName { get; set; }
    public string? RollNumber { get; set; }
    public decimal AttendancePercentage { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int LeaveCount { get; set; }
    public int HalfDayCount { get; set; }
    public int ConsecutiveAbsenceDays { get; set; }
}
