using Domain.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Certificates;

/// <summary>
/// Branch-wise "no dues" departments used for TC clearance (table: departments).
/// Not the same as <see cref="Department"/>, which maps the staff table "department".
/// </summary>
[Table("departments")]
[Index(nameof(BranchId))]
public partial class ClearanceDepartment : AuditableEntity
{
    [MaxLength(150)]
    [Required]
    public string DepartmentName { get; set; } = null!;
    [References(typeof(Branch))]
    public Guid BranchId { get; set; }
    [DefaultValue(true)]
    public bool? IsMandatory { get; set; }
}
