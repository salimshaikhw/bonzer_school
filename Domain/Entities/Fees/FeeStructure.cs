using Domain.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Fees;

[Table("fee_structures")]
public partial class FeeStructure : AuditableEntity
{

    [References(typeof(Class))]
    public Guid? ClassId { get; set; }

    [References(typeof(Division))]
    public Guid? DivisionId { get; set; }

    [References(typeof(FeeHead))]
    public Guid? FeeHeadId { get; set; }

    [Precision(10, 2)]
    public decimal? Amount { get; set; }
}
