using System;
using System.Collections.Generic;

namespace Application.ViewModels.Invoice;

/// <summary>Invoice lines plus the most recent payment breakdown (fee heads paid).</summary>
public class InvoiceStatementDto
{
    public InvoiceResponse Invoice { get; set; } = new();
    public List<InvoiceStatementLineDto> Lines { get; set; } = new();
    public InvoiceLastPaymentDto? LastPayment { get; set; }
}

public class InvoiceStatementLineDto
{
    public Guid FeeHeadId { get; set; }
    public string FeeHeadName { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
}

public class InvoiceLastPaymentDto
{
    public Guid PaymentId { get; set; }
    public DateOnly? PaymentDate { get; set; }
    public decimal? Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? TransactionNumber { get; set; }
    public List<InvoiceLastPaymentLineDto> FeeLines { get; set; } = new();
}

public class InvoiceLastPaymentLineDto
{
    public string Particulars { get; set; } = "";
    public decimal Amount { get; set; }
}
