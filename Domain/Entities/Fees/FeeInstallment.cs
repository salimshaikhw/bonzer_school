using Domain.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Fees;

[Table("fee_installments")]
public partial class FeeInstallment : AuditableEntity
{

    [References(typeof(Branch))]
    public Guid? BranchId { get; set; }

    [References(typeof(Class))]
    public Guid? ClassId { get; set; }

    [References(typeof(AcademicYear))]
    public Guid? AcademicYearId { get; set; }

    public int? InstallmentNo { get; set; }

    [MaxLength(50)]
    public string? InstallmentName { get; set; }

    public DateOnly? DueDate { get; set; }

    public DateOnly? LateFeeAfter { get; set; }
    [DefaultValue(0)]
    public int? GraceDays { get; set; }      
    [Precision(10, 2)]
    [DefaultValue(typeof(decimal), "0.00")]
    public decimal? LateFeePerDay { get; set; } 
}
