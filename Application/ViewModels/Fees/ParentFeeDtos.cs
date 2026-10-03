using System;
using System.Collections.Generic;

namespace Application.ViewModels.Fees;

public class ParentLinkedStudentDto
{
    public Guid StudentId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? AdmissionNo { get; set; }
    public string? RollNumber { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? ClassId { get; set; }
    public Guid? AcademicYearId { get; set; }
}

public class OpenInvoiceLineDto
{
    /// <summary>invoice_items.id — use when posting pay allocation.</summary>
    public Guid InvoiceItemId { get; set; }

    public Guid FeeHeadId { get; set; }
    public string FeeHeadName { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
}

public class OpenInvoiceDto
{
    public Guid Id { get; set; }
    public string? InvoiceNo { get; set; }
    public DateOnly? DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public List<OpenInvoiceLineDto> Lines { get; set; } = new();
}

public class PaymentHistoryRowDto
{
    public Guid PaymentId { get; set; }
    public string? InvoiceId { get; set; }
    public DateOnly? PaymentDate { get; set; }
    public decimal? Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? TransactionNumber { get; set; }
}

/// <summary>Line for parent/staff pay: allocate to an invoice item, by fee head, or as a named custom charge (receipt only).</summary>
public class ParentPayFeeLineDto
{
    public Guid? InvoiceItemId { get; set; }
    public Guid? FeeHeadId { get; set; }
    public string? CustomFeeName { get; set; }
    public decimal Amount { get; set; }
}

/// <summary>
/// Record payment against an invoice. Total paid is the sum of <see cref="FeeLines"/> amounts.
/// </summary>
public class ParentPayRequest
{
    public string InvoiceId { get; set; }
    public Guid AcademicYearId { get; set; }
    public string? PaymentMethod { get; set; }
    public string? TransactionReference { get; set; }
    public List<ParentPayFeeLineDto> FeeLines { get; set; } = new();
}

/// <summary>
/// Staff/parent collect: optionally create an open invoice from class fee map when none exists, then record payment.
/// </summary>
public class ParentCollectPayRequest
{
    public Guid AcademicYearId { get; set; }

    /// <summary>When set, pay this invoice. When null, uses first open invoice for the year or creates one from the schedule.</summary>
    public Guid? InvoiceId { get; set; }

    /// <summary>Passed to invoice creation when a new invoice is generated from the class fee map.</summary>
    public Guid? InstallmentId { get; set; }

    public string? PaymentMethod { get; set; }
    public string? TransactionReference { get; set; }
    public List<ParentPayFeeLineDto> FeeLines { get; set; } = new();
}
