using System;

namespace Application.ViewModels.Payment
{


    public class PaymentRequest
    {
        public string? InvoiceId { get; set; }
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
        public DateTime? CreatedAt { get; set; }
        public PaymentFeeLineRequest[]? FeeLines { get; set; }
    }

    public class PaymentFeeLineRequest
    {

        //public Guid PaymentId { get; set; }

        public Guid? FeeHeadId { get; set; }

        public decimal? Amount { get; set; }

        public Guid? InvoiceItemId { get; set; }
        public string? CustomFeeName { get; set; }

    }


    public class PaymentResponse
    {
        public Guid Id { get; set; }
        public string? InvoiceId { get; set; }
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
        public DateTime? CreatedAt { get; set; }
    }

    /// <summary>DTO for printing or re-printing a payment slip (one slip per payment).</summary>
    //public class PaymentReceiptDto
    //{
    //    public Guid PaymentId { get; set; }
    //    public DateOnly? PaymentDate { get; set; }
    //    public decimal? Amount { get; set; }
    //    public string? PaymentMethod { get; set; }
    //    public string? TransactionNumber { get; set; }
    //    public string? BankName { get; set; }
    //    public string? Remarks { get; set; }
    //    public DateTime? CreatedAt { get; set; }
    //    public string? RollNumber { get; set; }
    //    public Guid? ClassId { get; set; }
    //    public Guid? DivisionId { get; set; }
    //    public string? BranchName { get; set; }
    //    public ReceiptFeeLineDto[] FeeLines { get; set; } = Array.Empty<ReceiptFeeLineDto>();
    //}


    public class PaymentReceiptDto
    {
        public Guid PaymentId { get; set; }
        public string? InvoiceId { get; set; } // The 8-digit generated number
        public DateTime? PaymentDate { get; set; } // Matches the "date" type from Postgres
        public decimal? Amount { get; set; }
        public string? PaymentMethod { get; set; }
        public string? TransactionNumber { get; set; }
        public string? BankName { get; set; }
        public string? Remarks { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? RollNumber { get; set; }
        public Guid? ClassId { get; set; }
        public Guid? DivisionId { get; set; }
        public string? BranchName { get; set; }
        public ReceiptFeeLineDto[] FeeLines { get; set; } = Array.Empty<ReceiptFeeLineDto>();
    }

    public class ReceiptFeeLineDto
    {
        public string FeeHeadName { get; set; } = "";
        public decimal Amount { get; set; }
    }
}
