using Domain.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Attendances;

[Table("attendance_date_locks")]
public partial class AttendanceDateLock : AuditableEntity
{
    [References(typeof(Class))]
    public Guid? ClassId { get; set; }
    [References(typeof(Division))]
    public Guid? DivisionId { get; set; }
    public DateOnly? AttendanceDate { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? LockedAt { get; set; }
    public DateTime? UnlockedAt { get; set; }
}
