using Domain.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Fees;

[Index(nameof(InvoiceId))]
[Table("invoice_items")]
public partial class InvoiceItem : AuditableEntity
{

    [References(typeof(Invoice))]
    public Guid InvoiceId { get; set; }

    [References(typeof(FeeHead))]
    public Guid FeeHeadId { get; set; }

    [References(typeof(FeeInstallment))]
    public Guid? InstallmentId { get; set; }

    [Precision(10, 2)]
    public decimal TotalAmount { get; set; }

    [Precision(10, 2)]
    public decimal PaidAmount { get; set; }

    [Precision(10, 2)]
    public decimal RemainingAmount { get; set; }
}

