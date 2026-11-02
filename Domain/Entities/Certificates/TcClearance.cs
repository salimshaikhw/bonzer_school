using Domain.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Certificates;

[Table("tc_clearances")]
[Index(nameof(TcId), nameof(DepartmentId), IsUnique = true)]
[Index(nameof(TcId))]
[Index(nameof(Status))]
public partial class TcClearance : AuditableEntity
{
    [References(typeof(TcCertificate))]
    public Guid TcId { get; set; }
    [References(typeof(Student))]
    public Guid StudentId { get; set; }
    [References(typeof(ClearanceDepartment))]
    public Guid DepartmentId { get; set; }
    /// <summary>PENDING / CLEARED / REJECTED</summary>
    [MaxLength(30)]
    [DefaultValue("PENDING")]
    public string Status { get; set; } = null!;
    [Precision(10, 2)]
    [DefaultValue(typeof(decimal), "0")]
    public decimal? DueAmount { get; set; }
    public string? Remarks { get; set; }
    public Guid? ClearedBy { get; set; }
    public DateTime? ClearedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
