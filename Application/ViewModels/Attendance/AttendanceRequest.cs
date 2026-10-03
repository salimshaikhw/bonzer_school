using System;

namespace Application.ViewModels.Attendance
{
    public class AttendanceRequest
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
        public DateTime? CreatedAt { get; set; }
    }
}
