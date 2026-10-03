namespace Application.ViewModels.Attendance;

public class DropoutRiskDto
{
    public string RiskLevel { get; set; } = null!; // Low, Medium, High, Critical
    public decimal RiskScore { get; set; } // 0-100
    public List<string> Factors { get; set; } = new();
    public int StudentsAtRiskCount { get; set; }
    public List<ClassStudentAttendanceSummaryDto> StudentsAtRisk { get; set; } = new();
}
