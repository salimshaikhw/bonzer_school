using Domain.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Fees;

[Table("class_fee_map")]
public partial class ClassFeeMap : AuditableEntity
{

    [References(typeof(Class))]
    public Guid? ClassId { get; set; }

    [References(typeof(Division))]
    public Guid? DivisionId { get; set; }

    [References(typeof(AcademicYear))]
    public Guid? AcademicYearId { get; set; }

    [References(typeof(FeeHead))]
    public Guid? FeeHeadId { get; set; }

    [Precision(10, 2)]
    public decimal? Amount { get; set; }

    public int? InstallmentNo { get; set; }

    [MaxLength(20)]
    public string? AcademicYear { get; set; }
}
