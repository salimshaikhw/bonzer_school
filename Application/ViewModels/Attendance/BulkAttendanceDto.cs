using System;
using System.Collections.Generic;

namespace Application.ViewModels.Attendance
{
    public class BulkAttendanceDto
    {
        public Guid? ClassId { get; set; }
        public Guid? DivisionId { get; set; }
        public Guid? ClassTeacherId { get; set; }
        public DateOnly? AttendanceDate { get; set; }
        public List<MarkAttendanceDto> Entries { get; set; } = new List<MarkAttendanceDto>();
    }
}
