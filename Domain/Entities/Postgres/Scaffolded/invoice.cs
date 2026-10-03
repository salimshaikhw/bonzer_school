using System;
using System.Collections.Generic;

namespace Infrastructure;

public partial class Invoice
{
    public Guid Id { get; set; }
    public Guid? StudentId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? ClassId { get; set; }
    public Guid? DivisionId { get; set; }
    public Guid? InstallmentId { get; set; }
    public DateOnly? NextInstallmentDate { get; set; }
    public string? InvoiceNo { get; set; }
    public DateOnly? InvoiceDate { get; set; }
    public decimal? TotalAmount { get; set; }
    public decimal? PaidAmount { get; set; }
    public decimal? RemainingAmount { get; set; }
    public DateOnly? DueDate { get; set; }
    public string? Status { get; set; }
    public int? Month { get; set; }
    public int? Year { get; set; }
    public DateTime? CreatedAt { get; set; }
}
