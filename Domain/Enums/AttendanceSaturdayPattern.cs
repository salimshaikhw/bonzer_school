namespace Domain.Enums;

/// <summary>
/// Which Saturdays count as weekly off for attendance (per branch). Sunday uses <c>AttendanceSundayOff</c>.
/// </summary>
public enum AttendanceSaturdayPattern : short
{
    /// <summary>All Saturdays are working days (unless marked as calendar holiday).</summary>
    None = 0,

    /// <summary>Every Saturday is off.</summary>
    AllSaturdays = 1,

    /// <summary>2nd and 4th Saturday of each calendar month are off.</summary>
    SecondFourth = 2,

    /// <summary>1st, 3rd, and 5th Saturday of each calendar month are off (when a 5th exists).</summary>
    FirstThirdFifth = 3
}
