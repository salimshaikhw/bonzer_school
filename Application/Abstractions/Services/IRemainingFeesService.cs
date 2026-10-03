using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.ViewModels.Fees;

namespace Application.Abstractions.Services
{
    /// <summary>
    /// Service interface for calculating and retrieving remaining fees information
    /// </summary>
    public interface IRemainingFeesService
    {
        /// <summary>
        /// Get remaining fees for a specific student
        /// </summary>
        /// <param name="studentId">The ID of the student</param>
        /// <returns>Student remaining fees summary</returns>
        Task<StudentRemainingFeesDto> GetStudentRemainingFeesAsync(Guid studentId);

        /// <summary>
        /// Get class-wise remaining fees
        /// </summary>
        /// <param name="classId">The ID of the class</param>
        /// <returns>Class remaining fees summary</returns>
        Task<ClassRemainingFeesDto> GetClassRemainingFeesAsync(Guid classId);

        /// <summary>
        /// Get all classes remaining fees
        /// </summary>
        /// <returns>List of all classes with remaining fees</returns>
        Task<IEnumerable<ClassRemainingFeesDto>> GetAllClassesRemainingFeesAsync();

        /// <summary>
        /// Get division-wise remaining fees
        /// </summary>
        /// <param name="divisionId">The ID of the division</param>
        /// <returns>Division remaining fees summary</returns>
        Task<DivisionRemainingFeesDto> GetDivisionRemainingFeesAsync(Guid divisionId);

        /// <summary>
        /// Get all divisions remaining fees
        /// </summary>
        /// <returns>List of all divisions with remaining fees</returns>
        Task<IEnumerable<DivisionRemainingFeesDto>> GetAllDivisionsRemainingFeesAsync();

        /// <summary>
        /// Get branch-wise remaining fees
        /// </summary>
        /// <param name="branchId">The ID of the branch</param>
        /// <returns>Branch remaining fees summary</returns>
        Task<BranchRemainingFeesDto> GetBranchRemainingFeesAsync(Guid branchId);

        /// <summary>
        /// Get all branches remaining fees
        /// </summary>
        /// <returns>List of all branches with remaining fees</returns>
        Task<IEnumerable<BranchRemainingFeesDto>> GetAllBranchesRemainingFeesAsync();

        /// <summary>
        /// Get detailed fee breakdown for a student by fee head
        /// </summary>
        /// <param name="studentId">The ID of the student</param>
        /// <returns>List of fee details by fee head</returns>
        Task<IEnumerable<StudentFeeDetailDto>> GetStudentFeeDetailsAsync(Guid studentId);

        /// <summary>
        /// Get all students with pending fees in a division
        /// </summary>
        /// <param name="divisionId">The ID of the division</param>
        /// <returns>List of students with pending fees</returns>
        Task<IEnumerable<PendingFeesStudentDto>> GetPendingFeesStudentsByDivisionAsync(Guid divisionId);

        /// <summary>
        /// Get all students with pending fees in a class
        /// </summary>
        /// <param name="classId">The ID of the class</param>
        /// <returns>List of students with pending fees</returns>
        Task<IEnumerable<PendingFeesStudentDto>> GetPendingFeesStudentsByClassAsync(Guid classId);

        /// <summary>
        /// Get all students with pending fees in a branch
        /// </summary>
        /// <param name="branchId">The ID of the branch</param>
        /// <returns>List of students with pending fees</returns>
        Task<IEnumerable<PendingFeesStudentDto>> GetPendingFeesStudentsByBranchAsync(Guid branchId);

        /// <summary>
        /// Get monthly payment collection summary
        /// </summary>
        /// <param name="monthsBack">Number of months to look back (default 12)</param>
        /// <returns>List of monthly payment summaries</returns>
        Task<IEnumerable<MonthlyPaymentSummaryDto>> GetMonthlyPaymentSummaryAsync(int monthsBack = 12);

        /// <summary>
        /// Get comprehensive fee analytics dashboard
        /// </summary>
        /// <returns>Fee analytics dashboard data</returns>
        Task<FeeAnalyticsDashboardDto> GetFeeAnalyticsDashboardAsync();

        /// <summary>
        /// Get detailed fee breakdown for a student by payment head
        /// </summary>
        /// <param name="studentId">The ID of the student</param>
        /// <returns>List of fee details by payment head/installment</returns>
        Task<IEnumerable<StudentFeesByPaymentHeadDto>> GetStudentFeesByPaymentHeadAsync(Guid studentId);

        /// <summary>
        /// Get student payment history by fee head
        /// </summary>
        /// <param name="studentId">The ID of the student</param>
        /// <returns>List of payment transactions with fee head details</returns>
        Task<IEnumerable<StudentPaymentHistoryDto>> GetStudentPaymentHistoryByFeeHeadAsync(Guid studentId);

        /// <summary>
        /// Get class-wise payment head analysis
        /// </summary>
        /// <param name="classId">The ID of the class</param>
        /// <returns>Payment head analysis for all students in class</returns>
        Task<IEnumerable<ClassPaymentHeadAnalysisDto>> GetClassPaymentHeadAnalysisAsync(Guid classId);

        /// <summary>
        /// Get division-wise payment head analysis
        /// </summary>
        /// <param name="divisionId">The ID of the division</param>
        /// <returns>Payment head analysis for all students in division</returns>
        Task<IEnumerable<DivisionPaymentHeadAnalysisDto>> GetDivisionPaymentHeadAnalysisAsync(Guid divisionId);

        /// <summary>
        /// Get payment status matrix for division (paid/pending/no fee)
        /// </summary>
        /// <param name="divisionId">The ID of the division</param>
        /// <returns>Payment status for each student and fee head</returns>
        Task<IEnumerable<PaymentStatusMatrixDto>> GetPaymentStatusMatrixByDivisionAsync(Guid divisionId);

        /// <summary>
        /// Get outstanding fees by payment head (for collection prioritization)
        /// </summary>
        /// <returns>List of fee heads with outstanding balances</returns>
        Task<IEnumerable<OutstandingFeesByHeadDto>> GetOutstandingFeesByHeadAsync();

        /// <summary>
        /// Get student fee payment compliance report for division
        /// </summary>
        /// <param name="divisionId">The ID of the division</param>
        /// <returns>Compliance report for all students</returns>
        Task<IEnumerable<StudentFeeComplianceReportDto>> GetStudentComplianceReportByDivisionAsync(Guid divisionId);
        Task<FeeAnalyticsDashboardDto> GetBranchFeeAnalyticsAsync(Guid branchId);
    }
}
