using Domain.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Certificates;

[Table("tc_workflow_log")]
[Index(nameof(TcId))]
[Index(nameof(ActionAt))]
public partial class TcWorkflowLog : BaseEntity
{
    [References(typeof(TcCertificate))]
    public Guid TcId { get; set; }
    [MaxLength(50)]
    public string? FromStatus { get; set; }
    [MaxLength(50)]
    [Required]
    public string ToStatus { get; set; } = null!;
    /// <summary>CREATED / DEPARTMENT_CLEARANCE / HEAD_CLERK / CLASS_TEACHER / PRINCIPAL / REJECTED / ISSUED</summary>
    [MaxLength(40)]
    public string? ActorRole { get; set; }
    [References(typeof(Staff))]
    public Guid? ActionByStaffId { get; set; }
    [DefaultValueSql("CURRENT_TIMESTAMP")]
    public DateTime ActionAt { get; set; }
    public string? Remarks { get; set; }
    /// <summary>Set when ActorRole = DEPARTMENT_CLEARANCE.</summary>
    [References(typeof(ClearanceDepartment))]
    public Guid? DepartmentId { get; set; }
}
