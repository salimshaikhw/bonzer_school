using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Organization;

[Table("academic_years")]
public partial class AcademicYear : AuditableEntity
{

    [MaxLength(20)]
    public string? YearName { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    [DefaultValue(true)]
    public bool? IsActive { get; set; }
}
