using System;
using System.Collections.Generic;

namespace Infrastructure;

public partial class InvoiceItem
{
    public Guid Id { get; set; }

    public Guid InvoiceId { get; set; }

    public Guid FeeHeadId { get; set; }

    public Guid? InstallmentId { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal RemainingAmount { get; set; }

    public DateTime? CreatedAt { get; set; }
}

