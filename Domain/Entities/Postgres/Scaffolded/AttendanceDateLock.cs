using System;

namespace Domain.Entities.Postgres.Scaffolded;

public partial class AttendanceDateLock
{
    public Guid Id { get; set; }
    public Guid? ClassId { get; set; }
    public Guid? DivisionId { get; set; }
    public DateOnly? AttendanceDate { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? LockedAt { get; set; }
    public DateTime? UnlockedAt { get; set; }
}
