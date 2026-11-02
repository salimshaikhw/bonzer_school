using Domain.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Fees;

[Table("invoices")]
[Index(nameof(DueDate))]
[Index(nameof(StudentId))]
public partial class Invoice : AuditableEntity
{
    [References(typeof(Student))]
    public Guid? StudentId { get; set; }
    [References(typeof(Branch))]
    public Guid? BranchId { get; set; }
    [References(typeof(Class))]
    public Guid? ClassId { get; set; }
    [References(typeof(Division))]
    public Guid? DivisionId { get; set; }
    [References(typeof(FeeInstallment))]
    public Guid? InstallmentId { get; set; }
    public DateOnly? NextInstallmentDate { get; set; }
    [MaxLength(50)]
    public string? InvoiceNo { get; set; }
    public DateOnly? InvoiceDate { get; set; }
    [Precision(10, 2)]
    public decimal? TotalAmount { get; set; }
    [Precision(10, 2)]
    [DefaultValueSql("0")]
    public decimal? PaidAmount { get; set; }
    [Precision(10, 2)]
    public decimal? RemainingAmount { get; set; }
    public DateOnly? DueDate { get; set; }
    [MaxLength(20)]
    public string? Status { get; set; }
    public int? Month { get; set; }
    public int? Year { get; set; }
}
