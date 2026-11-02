using Domain.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Academics;

[Table("timetable")]
// One subject per class/division slot per academic year.
[Index(nameof(ClassId), nameof(DivisionId), nameof(AcademicYearId), nameof(DayOfWeek), nameof(PeriodNo), IsUnique = true)]
public partial class Timetable : AuditableEntity
{

    [References(typeof(Branch))]
    public Guid? BranchId { get; set; }

    [References(typeof(Class))]
    public Guid? ClassId { get; set; }

    [References(typeof(Subject))]
    public Guid? SubjectId { get; set; }

    public Guid? TeacherId { get; set; }
    [References(typeof(Division))]
    public Guid? DivisionId { get; set; }

    public int? DayOfWeek { get; set; }

    public int? PeriodNo { get; set; }

    [References(typeof(AcademicYear))]
    public Guid? AcademicYearId { get; set; }
}
