using System;

namespace Application.ViewModels.Attendance;

public class AttendanceTrendPointDto
{
    public DateOnly Date { get; set; }
    public string Status { get; set; } = null!;
    public bool IsPresent => string.Equals(Status, "Present", StringComparison.OrdinalIgnoreCase);
    public bool IsAbsent => string.Equals(Status, "Absent", StringComparison.OrdinalIgnoreCase);
    public bool IsLeave => string.Equals(Status, "Leave", StringComparison.OrdinalIgnoreCase);
    public bool IsHalfDay => string.Equals(Status, "HalfDay", StringComparison.OrdinalIgnoreCase);
    public bool IsLate => string.Equals(Status, "Late", StringComparison.OrdinalIgnoreCase);
}
