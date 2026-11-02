using Domain.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Attendances;

[Table("attendance")]
[SoftDelete]
[Index(nameof(AttendanceDate))]
[Index(nameof(DivisionId), nameof(AttendanceDate))]
// Partial unique index, so a soft-deleted mark does not block re-marking the same student/date.
[FilteredIndex(nameof(StudentId), nameof(AttendanceDate), Filter = "is_deleted IS NOT TRUE", IsUnique = true)]
public partial class Attendance : AuditableEntity
{

    [References(typeof(Student))]
    public Guid? StudentId { get; set; }

    public Guid? ClassTeacherId { get; set; }

    [References(typeof(Class))]
    public Guid? ClassId { get; set; }

    [References(typeof(Division))]
    public Guid? DivisionId { get; set; }

    public DateOnly? AttendanceDate { get; set; }

    [MaxLength(20)]
    public string? Status { get; set; }

    [MaxLength(50)]
    public string? LeaveType { get; set; }

    [DefaultValue(false)]
    public bool? IsHoliday { get; set; }

    public string? Remark { get; set; }


    [DefaultValue(false)]
    public bool? IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }
}
