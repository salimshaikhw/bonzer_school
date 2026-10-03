using System;

namespace Application.ViewModels.Fees
{
    /// <summary>
    /// DTO for student remaining fees summary
    /// </summary>
    public class StudentRemainingFeesDto
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; }
        public string AdmissionNo { get; set; }
        public Guid? ClassId { get; set; }
        public Guid? DivisionId { get; set; }
        public Guid? BranchId { get; set; }
        public decimal TotalDueFees { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingFees { get; set; }
        
        public decimal PercentagePaid => TotalDueFees > 0 
            ? Math.Round((PaidAmount / TotalDueFees) * 100, 2) 
            : 0;
    }

    /// <summary>
    /// DTO for class-wise remaining fees summary
    /// </summary>
    public class ClassRemainingFeesDto
    {
        public Guid ClassId { get; set; }
        public string ClassName { get; set; }
        public Guid? BranchId { get; set; }
        public int TotalStudents { get; set; }
        public decimal TotalClassDueFees { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public decimal TotalRemainingFees { get; set; }
        public decimal PercentagePaid { get; set; }
    }

    /// <summary>
    /// DTO for division-wise remaining fees summary
    /// </summary>
    public class DivisionRemainingFeesDto
    {
        public Guid DivisionId { get; set; }
        public string DivisionName { get; set; }
        public string DivisionCode { get; set; }
        public string ClassName { get; set; }
        public Guid? BranchId { get; set; }
        public int TotalStudents { get; set; }
        public decimal TotalDivisionDueFees { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public decimal TotalRemainingFees { get; set; }
        public decimal PercentagePaid { get; set; }
    }

    /// <summary>
    /// DTO for branch-wise remaining fees summary
    /// </summary>
    public class BranchRemainingFeesDto
    {
        public Guid BranchId { get; set; }
        public string BranchName { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Phone { get; set; }
        public int TotalStudents { get; set; }
        public int TotalClasses { get; set; }
        public decimal TotalBranchDueFees { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public decimal TotalRemainingFees { get; set; }
        public decimal PercentagePaid { get; set; }
    }

    /// <summary>
    /// DTO for detailed student fees breakdown by fee head
    /// </summary>
    public class StudentFeeDetailDto
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; }
        public Guid? FeeHeadId { get; set; }
        public string FeeName { get; set; }
        public int? Installment { get; set; }
        public decimal DueFees { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingFees { get; set; }
        
        public decimal PercentagePaid => DueFees > 0 
            ? Math.Round((PaidAmount / DueFees) * 100, 2) 
            : 0;
    }

    /// <summary>
    /// DTO for students with pending fees in a division
    /// </summary>
    public class PendingFeesStudentDto
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; }
        public string AdmissionNo { get; set; }
        public string DivisionName { get; set; }
        public string ClassName { get; set; }
        public Guid? BranchId { get; set; }
        public decimal TotalDueFees { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingFees { get; set; }
        
        public decimal PercentagePaid => TotalDueFees > 0 
            ? Math.Round((PaidAmount / TotalDueFees) * 100, 2) 
            : 0;
    }

    /// <summary>
    /// DTO for monthly payment collection summary
    /// </summary>
    public class MonthlyPaymentSummaryDto
    {
        public DateTime PaymentMonth { get; set; }
        public string BranchName { get; set; }
        public int StudentsWithPayments { get; set; }
        public int TotalPayments { get; set; }
        public decimal TotalCollected { get; set; }
    }

    /// <summary>
    /// DTO for fee analytics dashboard
    /// </summary>
    public class FeeAnalyticsDashboardDto
    {
        public decimal TotalDueFees { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public decimal TotalRemainingFees { get; set; }
        public decimal OverallPercentagePaid { get; set; }
        public int TotalStudents { get; set; }
        public int StudentsWithPendingFees { get; set; }
        
        public int TotalBranches { get; set; }
        public int TotalClasses { get; set; }
        public int TotalDivisions { get; set; }
        
        public List<ClassRemainingFeesDto> TopClassesByRemainingFees { get; set; }
        public List<BranchRemainingFeesDto> BranchWiseSummary { get; set; }
        public List<MonthlyPaymentSummaryDto> MonthlyPaymentTrend { get; set; }
    }

    /// <summary>
    /// DTO for student fees breakdown by payment head/installment with summary
    /// </summary>
    public class StudentFeesByPaymentHeadDto
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; }
        public string AdmissionNo { get; set; }
        public Guid? ClassId { get; set; }
        public Guid? DivisionId { get; set; }
        public Guid? BranchId { get; set; }
        
        // Summary totals (SaaS-like display)
        public decimal TotalDueFees { get; set; }
        public decimal TotalPaidFees { get; set; }
        public decimal TotalRemainingFees { get; set; }
        public decimal OverallPercentagePaid { get; set; }
        
        // Individual fee head details
        public Guid? FeeHeadId { get; set; }
        public string FeeHeadName { get; set; }
        public string AccountCode { get; set; }
        public int? Installment { get; set; }
        public decimal DueFeeAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingFees { get; set; }
        public int PaymentCount { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public string LastPaymentMethod { get; set; }
        
        public decimal PercentagePaid => DueFeeAmount > 0 
            ? Math.Round((PaidAmount / DueFeeAmount) * 100, 2) 
            : 0;
    }

    /// <summary>
    /// DTO for student payment history by fee head with detailed transaction info
    /// </summary>
    public class StudentPaymentHistoryDto
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; }
        public string AdmissionNo { get; set; }
        public string ClassName { get; set; }
        public string DivisionName { get; set; }
        
        // Transaction details
        public Guid PaymentId { get; set; }
        public string InvoiceId { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string PaymentYear { get; set; }
        public string PaymentMonth { get; set; }
        
        // Payment method details
        public string PaymentMethod { get; set; }
        public string BankName { get; set; }
        public string ChequeNo { get; set; }
        public string TransactionReference { get; set; }
        
        // Fee details
        public Guid? FeeHeadId { get; set; }
        public string FeeHeadName { get; set; }
        public decimal PaidAmount { get; set; }
        public int? Installment { get; set; }
        
        // Additional info for parent/user view
        public string PaymentStatus { get; set; } // COMPLETED, PENDING, FAILED
        public string Remarks { get; set; }
        public string ReceiptUrl { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    /// <summary>
    /// DTO for class-wise payment head analysis
    /// </summary>
    public class ClassPaymentHeadAnalysisDto
    {
        public Guid ClassId { get; set; }
        public string ClassName { get; set; }
        public Guid? FeeHeadId { get; set; }
        public string FeeHeadName { get; set; }
        public int TotalStudents { get; set; }
        public decimal TotalDueAmount { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public decimal TotalRemainingFees { get; set; }
        public decimal PercentagePaid { get; set; }
    }

    /// <summary>
    /// DTO for division-wise payment head analysis
    /// </summary>
    public class DivisionPaymentHeadAnalysisDto
    {
        public Guid DivisionId { get; set; }
        public string DivisionName { get; set; }
        public string DivisionCode { get; set; }
        public string ClassName { get; set; }
        public Guid? FeeHeadId { get; set; }
        public string FeeHeadName { get; set; }
        public int TotalStudents { get; set; }
        public decimal TotalDueAmount { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public decimal TotalRemainingFees { get; set; }
        public decimal PercentagePaid { get; set; }
    }

    /// <summary>
    /// DTO for payment status matrix (paid/pending/no fee status)
    /// </summary>
    public class PaymentStatusMatrixDto
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; }
        public string AdmissionNo { get; set; }
        public Guid? FeeHeadId { get; set; }
        public string FeeHeadName { get; set; }
        public string PaymentStatus { get; set; } // PAID, PENDING, NO FEE
        public decimal DueAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
    }

    /// <summary>
    /// DTO for outstanding fees by fee head (for collection prioritization)
    /// </summary>
    public class OutstandingFeesByHeadDto
    {
        public Guid FeeHeadId { get; set; }
        public string FeeHeadName { get; set; }
        public string AccountCode { get; set; }
        public int StudentsAffected { get; set; }
        public decimal TotalDueAmount { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public decimal TotalOutstandingAmount { get; set; }
        public decimal OverallPercentagePaid { get; set; }
    }

    /// <summary>
    /// DTO for student fee payment compliance report
    /// </summary>
    public class StudentFeeComplianceReportDto
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; }
        public string AdmissionNo { get; set; }
        public string ClassName { get; set; }
        public string DivisionName { get; set; }
        public decimal TotalDueAmount { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public decimal TotalRemainingFees { get; set; }
        public decimal OverallPercentagePaid { get; set; }
        public int FeeHeadsPaid { get; set; }
        public int FeeHeadsPending { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public int TotalPayments { get; set; }
        
        public string ComplianceStatus
        {
            get
            {
                if (OverallPercentagePaid >= 100) return "FULLY PAID";
                if (OverallPercentagePaid >= 75) return "MOSTLY PAID";
                if (OverallPercentagePaid >= 50) return "PARTIALLY PAID";
                return "MOSTLY PENDING";
            }
        }
    }

    /// <summary>
    /// Response model for remaining fees API
    /// </summary>
    public class RemainingFeesResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public dynamic Data { get; set; }
        public DateTime? Timestamp { get; set; } = DateTime.UtcNow;
    }
}
