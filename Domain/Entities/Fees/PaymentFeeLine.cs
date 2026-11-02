using Domain.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Fees;

[Table("payment_fee_lines")]
public partial class PaymentFeeLine : AuditableEntity
{
    [References(typeof(Payment))]
    public Guid PaymentId { get; set; }

    /// <summary>Null for ad-hoc lines recorded with <see cref="CustomFeeName"/> only.</summary>
    [References(typeof(FeeHead))]
    public Guid? FeeHeadId { get; set; }

    /// <summary>The invoice item this line pays, when the payment settles an invoice (its balance is reduced).</summary>
    [References(typeof(InvoiceItem))]
    public Guid? InvoiceItemId { get; set; }

    [Precision(10, 2)]
    public decimal Amount { get; set; }

    [MaxLength(255)]
    public string? CustomFeeName { get; set; }
}
