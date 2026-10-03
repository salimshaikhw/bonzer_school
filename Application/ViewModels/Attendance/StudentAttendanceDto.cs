using System;

namespace Application.ViewModels.Attendance
{
    public class StudentAttendanceDto
    {
        public Guid StudentId { get; set; }
        public string? Status { get; set; }
        public string? LeaveType { get; set; }
        public string? Remark { get; set; }
    }
}
