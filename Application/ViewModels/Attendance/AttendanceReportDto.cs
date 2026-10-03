namespace Application.ViewModels.Attendance;

public class AttendanceReportDto
{
    public AttendancePeriodType PeriodType { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public AttendanceSummaryDto Summary { get; set; } = null!;
    public IEnumerable<AttendanceResponse> DetailedList { get; set; } = null!;
}
