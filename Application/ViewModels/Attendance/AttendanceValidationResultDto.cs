namespace Application.ViewModels.Attendance
{
    /// <summary>Result of an attendance validation check.</summary>
    public class AttendanceValidationResultDto
    {
        public bool IsValid { get; set; }
        public string? Message { get; set; }
    }
}
