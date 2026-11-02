using Domain.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Attendances;

[Table("attendance_academic_year_locks")]
public partial class AttendanceAcademicYearLock : AuditableEntity
{
    [References(typeof(AcademicYear))]
    public Guid? AcademicYearId { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? LockedAt { get; set; }
    public DateTime? UnlockedAt { get; set; }
}
