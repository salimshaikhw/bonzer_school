using Domain.DataAnnotations;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Attendances;

[Table("school_holidays")]
[SoftDelete]
[FilteredIndex(nameof(BranchId), nameof(HolidayDate), Filter = "is_deleted IS NOT TRUE", IsUnique = true)]
public partial class SchoolHoliday : AuditableEntity
{

    [References(typeof(Branch), CascadeDelete = true)]
    public Guid BranchId { get; set; }

    public DateOnly HolidayDate { get; set; }

    [MaxLength(200)]
    public string? Name { get; set; }


    [DefaultValue(false)]
    public bool? IsDeleted { get; set; }
}
