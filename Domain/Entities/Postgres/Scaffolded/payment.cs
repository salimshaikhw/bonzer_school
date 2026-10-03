using System;
using System.Collections.Generic;

namespace Domain.Entities.Postgres.Scaffolded;

public partial class Payment
{
    public Guid Id { get; set; }

    public String? InvoiceId { get; set; }

    public Guid? StudentId { get; set; }

    public Guid? BranchId { get; set; }

    public Guid? DivisionId { get; set; }

    public Guid? CollectedByStaffId { get; set; }

    public DateOnly? PaymentDate { get; set; }

    public decimal? Amount { get; set; }

    public string? PaymentMethod { get; set; }

    public string? PaymentType { get; set; }

    public string? TransactionNumber { get; set; }

    public string? BankName { get; set; }

    public string? Remarks { get; set; }
    public Guid? InstallmentId { get; set; }

    public DateTime? CreatedAt { get; set; }
}
