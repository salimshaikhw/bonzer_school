using System;

namespace Domain.Entities.Postgres.Scaffolded;

public partial class AttendanceAcademicYearLock
{
    public Guid Id { get; set; }
    public Guid? AcademicYearId { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? LockedAt { get; set; }
    public DateTime? UnlockedAt { get; set; }
}
