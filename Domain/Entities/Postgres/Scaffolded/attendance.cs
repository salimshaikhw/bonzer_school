using System;
using System.Collections.Generic;

namespace Infrastructure;

public partial class Attendance
{
    public Guid Id { get; set; }

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

    public bool? IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }
}
