using Domain.Enums;
using Domain.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Organization;

[Table("branches")]
public partial class Branch : AuditableEntity
{
    /// <summary>Id of the school group (catalog organizations row; shared database, so no foreign key constraint).</summary>
    public Guid? GroupId { get; set; }

    /// <summary>Id of a catalog institution_types row (shared database, so no foreign key constraint).</summary>
    public Guid? InstitutionTypeId { get; set; }

    /// <summary>Id of a catalog mediums row (shared database, so no foreign key constraint).</summary>
    public Guid? MediumId { get; set; }

    public string? SchoolUid { get; set; }

    public string? Name { get; set; }

    public string? Address { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    /// <summary>When true, Saturdays are treated as weekly off for attendance stats and optional marking.</summary>

    /// <remarks>Superseded by <see cref="AttendanceSaturdayPattern"/> when that is not <see cref="AttendanceSaturdayPattern.None"/>; kept for backward compatibility.</remarks>

    public bool? AttendanceSaturdayOff { get; set; }

    /// <summary>Which Saturdays are weekly off (none / all / 2nd&amp;4th / 1st&amp;3rd&amp;5th).</summary>

    public AttendanceSaturdayPattern AttendanceSaturdayPattern { get; set; }

    /// <summary>When true or null (default), Sundays are weekly off. Set false only if Sunday is a working day.</summary>

    public bool? AttendanceSundayOff { get; set; }
}

