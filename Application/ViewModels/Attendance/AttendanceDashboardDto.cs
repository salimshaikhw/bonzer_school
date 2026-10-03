namespace Application.ViewModels.Attendance;

public class AttendanceDashboardDto
{
    public int TodayPresent { get; set; }
    public int TodayAbsent { get; set; }
    public int TodayLeave { get; set; }
    public decimal TodayPercentage { get; set; }
    public int TotalStudents { get; set; }
    public AttendanceSummaryDto? MonthSummary { get; set; }
    public int PendingLeaveApprovals { get; set; }
    public int PendingCorrectionRequests { get; set; }
}
