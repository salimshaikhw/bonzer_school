using System;
using System.Collections.Generic;
using Domain.Enums;

namespace Domain.Entities.Postgres.Scaffolded;

public partial class Branch
{
    public Guid Id { get; set; }

    public Guid? GroupId { get; set; }

    public Guid? InstitutionTypeId { get; set; }

    public Guid? MediumId { get; set; }

    public string? Schooluid { get; set; }

    public string? Name { get; set; }

    public string? Address { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public DateTime? CreatedAt { get; set; }

    /// <summary>When true, Saturdays are treated as weekly off for attendance stats and optional marking.</summary>
    /// <remarks>Superseded by <see cref="AttendanceSaturdayPattern"/> when that is not <see cref="AttendanceSaturdayPattern.None"/>; kept for backward compatibility.</remarks>
    public bool? AttendanceSaturdayOff { get; set; }

    /// <summary>Which Saturdays are weekly off (none / all / 2nd&amp;4th / 1st&amp;3rd&amp;5th).</summary>
    public AttendanceSaturdayPattern AttendanceSaturdayPattern { get; set; }

    /// <summary>When true or null (default), Sundays are weekly off. Set false only if Sunday is a working day.</summary>
    public bool? AttendanceSundayOff { get; set; }
}
