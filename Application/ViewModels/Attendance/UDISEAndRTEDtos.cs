namespace Application.ViewModels.Attendance;

public class UDISEReportDto
{
    public Guid BranchId { get; set; }
    public string? BranchName { get; set; }
    public int TotalEnrollment { get; set; }
    public int PresentToday { get; set; }
    public int AbsentToday { get; set; }
    public decimal AttendanceRate { get; set; }
    public int RteStudents { get; set; }
    public int RtePresent { get; set; }
    public decimal RteAttendanceRate { get; set; }
}

public class RTEComplianceDto
{
    public Guid BranchId { get; set; }
    public int RteSeatCount { get; set; }
    public int RteEnrolled { get; set; }
    public decimal AttendanceCompliancePercentage { get; set; }
    public bool IsCompliant { get; set; }
    public string? Notes { get; set; }
}
