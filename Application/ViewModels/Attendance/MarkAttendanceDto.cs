using System;

namespace Application.ViewModels.Attendance
{
    public class MarkAttendanceDto
    {
        public Guid? StudentId { get; set; }
        public Guid? ClassTeacherId { get; set; }
        public Guid? ClassId { get; set; }
        public Guid? DivisionId { get; set; }
        public DateOnly? AttendanceDate { get; set; }
        public string? Status { get; set; }
        public string? LeaveType { get; set; }
        public bool? IsHoliday { get; set; }
        public string? Remark { get; set; }
    }
}
