using System;

namespace Application.ViewModels.Attendance;

public class AttendanceWeeklyOffSettingsDto
{
    public Guid BranchId { get; set; }

    /// <summary>none | allSaturdays | secondFourth | firstThirdFifth</summary>
    public string SaturdayPattern { get; set; } = "none";

    public bool SundayOff { get; set; }
}

public class UpdateAttendanceWeeklyOffSettingsRequest
{
    /// <summary>none | allSaturdays | secondFourth | firstThirdFifth</summary>
    public string SaturdayPattern { get; set; } = "none";

    public bool SundayOff { get; set; }
}

public class SchoolHolidayResponse
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public DateOnly HolidayDate { get; set; }
    public string? Name { get; set; }
}

public class SchoolHolidayRequest
{
    public DateOnly HolidayDate { get; set; }
    public string? Name { get; set; }
}
