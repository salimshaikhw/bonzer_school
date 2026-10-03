namespace Application.ViewModels.Attendance
{
    public class UpdateAttendanceDto
    {
        public string? Status { get; set; }
        public string? LeaveType { get; set; }
        public bool? IsHoliday { get; set; }
        public string? Remark { get; set; }
    }
}
