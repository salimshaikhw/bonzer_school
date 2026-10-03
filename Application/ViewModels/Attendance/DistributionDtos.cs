namespace Application.ViewModels.Attendance;

public class ClassAttendanceComparisonDto
{
    public Guid ClassId { get; set; }
    public string? ClassName { get; set; }
    public decimal AttendancePercentage { get; set; }
    public int PresentCount { get; set; }
    public int TotalCount { get; set; }
}

public class GenderWiseAttendanceDistributionDto
{
    public string Gender { get; set; } = null!;
    public int StudentCount { get; set; }
    public decimal AttendancePercentage { get; set; }
    public int PresentCount { get; set; }
    public int TotalRecords { get; set; }
}

public class CategoryWiseAttendanceDistributionDto
{
    public string Category { get; set; } = null!;
    public int StudentCount { get; set; }
    public decimal AttendancePercentage { get; set; }
    public int PresentCount { get; set; }
    public int TotalRecords { get; set; }
}
