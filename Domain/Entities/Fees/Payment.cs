using Domain.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Fees;

[Table("payments")]
[Index(nameof(CollectedByStaffId))]
[Index(nameof(DivisionId))]
[Index(nameof(StudentId))]
// InvoiceId holds the generated receipt code; unique so concurrent payments cannot share one.
[FilteredIndex(nameof(InvoiceId), Filter = "invoice_id IS NOT NULL", IsUnique = true)]
public partial class Payment : AuditableEntity
{

    [MaxLength(50)]
    public String? InvoiceId { get; set; }

    [References(typeof(Student))]
    public Guid? StudentId { get; set; }

    [References(typeof(Branch))]
    public Guid? BranchId { get; set; }

    [References(typeof(Division))]
    public Guid? DivisionId { get; set; }

    [References(typeof(Staff))]
    public Guid? CollectedByStaffId { get; set; }

    public DateOnly? PaymentDate { get; set; }

    [Precision(10, 2)]
    public decimal? Amount { get; set; }

    [MaxLength(50)]
    public string? PaymentMethod { get; set; }

    [MaxLength(50)]
    public string? PaymentType { get; set; }

    [MaxLength(100)]
    public string? TransactionNumber { get; set; }

    [MaxLength(100)]
    public string? BankName { get; set; }

    public string? Remarks { get; set; }
    [References(typeof(FeeInstallment))]
    public Guid? InstallmentId { get; set; }
}
