using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.ViewModels.Attendance;

namespace Application.Abstractions.Services
{
    public interface IAttendanceService
    {
        // --- Division ---
        Task<AttendanceReportDto> GetDivisionAttendanceReportAsync(Guid divisionId, AttendancePeriodType periodType, DateTime? startDate, DateTime? endDate);
        Task<AttendanceSummaryDto> GetDivisionAttendanceSummaryAsync(Guid divisionId, AttendancePeriodType periodType, DateTime? startDate, DateTime? endDate);
        Task<IEnumerable<AttendanceResponse>> GetDivisionAttendanceDetailedListAsync(Guid divisionId, AttendancePeriodType periodType, DateTime? startDate, DateTime? endDate);
        Task<decimal> GetDivisionAttendancePercentageAsync(Guid divisionId, AttendancePeriodType periodType);
        Task<int> GetDivisionPresentCountAsync(Guid divisionId, AttendancePeriodType periodType);
        Task<int> GetDivisionAbsentCountAsync(Guid divisionId, AttendancePeriodType periodType);
        Task<int> GetDivisionLeaveCountAsync(Guid divisionId, AttendancePeriodType periodType);
        Task<IEnumerable<ClassStudentAttendanceSummaryDto>> GetDivisionLowAttendanceStudentsAsync(Guid divisionId, decimal threshold);
        Task<IEnumerable<ClassStudentAttendanceSummaryDto>> GetDivisionChronicAbsenteesAsync(Guid divisionId);
        Task<IEnumerable<AttendanceTrendPointDto>> GetDivisionAttendanceTrendAsync(Guid divisionId, AttendancePeriodType periodType);

        // --- Branch ---
        Task<AttendanceReportDto> GetBranchAttendanceReportAsync(Guid branchId, AttendancePeriodType periodType, DateTime? startDate, DateTime? endDate);
        Task<AttendanceSummaryDto> GetBranchAttendanceSummaryAsync(Guid branchId, AttendancePeriodType periodType);
        Task<decimal> GetBranchAttendancePercentageAsync(Guid branchId, AttendancePeriodType periodType);
        Task<IEnumerable<ClassStudentAttendanceSummaryDto>> GetBranchLowAttendanceStudentsAsync(Guid branchId, decimal threshold);
        Task<IEnumerable<ClassStudentAttendanceSummaryDto>> GetBranchChronicAbsenteesAsync(Guid branchId);
        Task<IEnumerable<AttendanceTrendPointDto>> GetBranchAttendanceTrendAsync(Guid branchId, AttendancePeriodType periodType);
        Task<IEnumerable<ClassAttendanceComparisonDto>> AttendanceComparisonBetweenClassesAsync(Guid branchId, AttendancePeriodType periodType);
        Task<IEnumerable<GenderWiseAttendanceDistributionDto>> GenderWiseAttendanceDistributionAsync(Guid branchId, AttendancePeriodType periodType);
        Task<IEnumerable<CategoryWiseAttendanceDistributionDto>> CategoryWiseAttendanceDistributionAsync(Guid branchId, AttendancePeriodType periodType);

        // --- Today & Dashboard ---
        Task<AttendanceSummaryDto> GetTodayAttendanceSummaryAsync(Guid branchId);
        Task<int> GetTodayPresentCountAsync(Guid branchId);
        Task<int> GetTodayAbsentCountAsync(Guid branchId);
        Task<decimal> GetTodayAttendancePercentageAsync(Guid branchId);
        Task<AttendanceDashboardDto> GetAttendanceDashboardDataAsync(Guid branchId);

        // --- Leave ---
        Task<LeaveRequestDto> ApplyStudentLeaveAsync(Guid studentId, DateTime fromDate, DateTime toDate, string leaveType, string? reason = null);
        Task<bool> ApproveLeaveRequestAsync(Guid leaveId, Guid? approvedBy = null);
        Task<bool> RejectLeaveRequestAsync(Guid leaveId, string? rejectionReason = null, Guid? rejectedBy = null);
        Task<IEnumerable<LeaveRequestDto>> GetStudentLeaveHistoryAsync(Guid studentId);
        Task<IEnumerable<LeaveRequestDto>> GetPendingLeaveApprovalsAsync(Guid classId);

        // --- Correction ---
        Task<AttendanceCorrectionRequestDto> RequestAttendanceCorrectionAsync(Guid attendanceId, string reason, Guid? requestedBy = null);
        Task<bool> ApproveAttendanceCorrectionAsync(Guid requestId, Guid? approvedBy = null);
        Task<bool> RejectAttendanceCorrectionAsync(Guid requestId, string? rejectionReason = null, Guid? rejectedBy = null);
        Task<IEnumerable<AttendanceCorrectionRequestDto>> GetAttendanceCorrectionRequestsAsync(Guid classId);
        Task AuditAttendanceChangeAsync(Guid attendanceId, string action, string? details = null);

        // --- Notifications ---
        Task NotifyParentOnAbsenceAsync(Guid studentId, DateTime date);
        Task NotifyParentOnConsecutiveAbsenceAsync(Guid studentId);
        Task NotifyClassTeacherLowAttendanceAsync(Guid studentId);
        Task NotifyPrincipalChronicAbsenteeAsync(Guid branchId);
        Task SendDailyAbsenceNotificationAsync(Guid branchId);

        // --- Export ---
        Task<ExportResultDto> ExportStudentAttendanceReportAsync(Guid studentId, AttendancePeriodType periodType);
        Task<ExportResultDto> ExportClassAttendanceReportAsync(Guid classId, AttendancePeriodType periodType);
        Task<ExportResultDto> ExportDivisionAttendanceReportAsync(Guid divisionId, AttendancePeriodType periodType);
        Task<ExportResultDto> ExportBranchAttendanceReportAsync(Guid branchId, AttendancePeriodType periodType);
        Task<ExportResultDto> GenerateAttendanceRegisterPDFAsync(Guid classId, int month, int year);
        Task<ExportResultDto> GenerateAttendanceExcelAsync(Guid branchId, AttendancePeriodType periodType);

        // --- Auto / Batch ---
        Task AutoMarkHolidayAttendanceAsync(DateTime date);
        Task AutoCloseAttendanceAfterTimeAsync(Guid classId);
        Task AutoCalculateAttendancePercentagesAsync(Guid academicYearId);
        Task AutoFlagLowAttendanceStudentsAsync(decimal threshold);
        Task AutoNotifyChronicAbsenteesAsync();
        Task RecalculateAttendanceStatisticsAsync(Guid academicYearId);
        Task<bool> FreezeAttendanceForAcademicYearAsync(Guid academicYearId);
        Task<object> ValidateAttendanceIntegrityAsync(Guid academicYearId);

        // --- UDISE / RTE ---
        Task<UDISEReportDto> GetUDISEAttendanceReportAsync(Guid branchId);
        Task<RTEComplianceDto> GetRTEAttendanceComplianceAsync(Guid branchId);

        Task<IEnumerable<AttendanceResponse>> GetAllAsync();
        Task<AttendanceResponse?> GetByIdAsync(Guid id);
        Task<AttendanceResponse> CreateAsync(AttendanceRequest request);
        Task<AttendanceResponse> UpdateAsync(Guid id, AttendanceRequest request);
        Task<bool> DeleteAsync(Guid id);
        Task<int> CommitAsync();

        Task<AttendanceResponse> MarkAttendanceAsync(MarkAttendanceDto dto);
        Task<IEnumerable<AttendanceResponse>> BulkMarkAttendanceAsync(BulkAttendanceDto dto);
        Task<BulkAttendanceImportPreviewDto> PreviewBulkAttendanceImportAsync(BulkAttendanceImportRequest request);
        Task<BulkAttendanceImportApplyResultDto> ImportBulkAttendanceFromMachineAsync(BulkAttendanceImportRequest request);
        Task<byte[]> GenerateDivisionAttendanceImportTemplateAsync(AttendanceDivisionTemplateRequest request);
        Task<IEnumerable<AttendanceResponse>> MarkClassAttendanceAsync(Guid classId, Guid divisionId, DateTime date, List<StudentAttendanceDto> students);
        Task<AttendanceResponse?> UpdateAttendanceAsync(Guid attendanceId, UpdateAttendanceDto dto);
        Task<bool> SoftDeleteAttendanceAsync(Guid attendanceId);
        Task<bool> HardDeleteAttendanceAsync(Guid attendanceId);
        Task<bool> RestoreAttendanceAsync(Guid attendanceId);

        Task<bool> LockAttendanceByDateAsync(Guid classId, Guid divisionId, DateTime date);
        Task<bool> UnlockAttendanceByDateAsync(Guid classId, Guid divisionId, DateTime date);
        Task<bool> LockAttendanceForAcademicYearAsync(Guid academicYearId);
        Task<bool> UnlockAttendanceForAcademicYearAsync(Guid academicYearId);
        Task<bool> ValidateAttendanceIsLockedAsync(Guid classId, DateTime date);

        Task<ValidationResultDto> ValidateAttendanceNotDuplicateAsync(Guid studentId, DateTime date);
        Task<ValidationResultDto> ValidateAttendanceWithinAcademicYearAsync(DateTime date, Guid academicYearId);
        Task<ValidationResultDto> ValidateStudentActiveAsync(Guid studentId);
        Task<ValidationResultDto> ValidateStudentBelongsToClassAsync(Guid studentId, Guid classId);
        Task<ValidationResultDto> ValidateDivisionMatchAsync(Guid studentId, Guid divisionId);
        Task<ValidationResultDto> ValidateHolidayAsync(DateTime date, Guid? branchId = null);
        Task<ValidationResultDto> ValidateAttendancePeriodAsync(AttendancePeriodType periodType, DateTime? startDate, DateTime? endDate);

        Task<AttendanceDateRangeDto> ResolveDateRangeAsync(AttendancePeriodType periodType, DateTime? startDate, DateTime? endDate, Guid? studentId = null);

        Task<AttendanceReportDto> GetStudentAttendanceReportAsync(Guid studentId, AttendancePeriodType periodType, DateTime? startDate, DateTime? endDate);
        Task<AttendanceSummaryDto> GetStudentAttendanceSummaryAsync(Guid studentId, AttendancePeriodType periodType, DateTime? startDate, DateTime? endDate);
        Task<IEnumerable<AttendanceResponse>> GetStudentAttendanceDetailedListAsync(Guid studentId, AttendancePeriodType periodType, DateTime? startDate, DateTime? endDate);
        Task<decimal> GetStudentAttendancePercentageAsync(Guid studentId, AttendancePeriodType periodType);
        Task<int> GetStudentPresentCountAsync(Guid studentId, AttendancePeriodType periodType);
        Task<int> GetStudentAbsentCountAsync(Guid studentId, AttendancePeriodType periodType);
        Task<int> GetStudentLeaveCountAsync(Guid studentId, AttendancePeriodType periodType);
        Task<int> GetStudentHalfDayCountAsync(Guid studentId, AttendancePeriodType periodType);
        Task<int> GetStudentConsecutiveAbsenceDaysAsync(Guid studentId);
        Task<int> GetStudentLateCountAsync(Guid studentId);
        Task<bool> IsStudentLowAttendanceAsync(Guid studentId, decimal threshold);
        Task<bool> IsStudentChronicAbsenteeAsync(Guid studentId, int consecutiveDays);
        Task<IEnumerable<AttendanceTrendPointDto>> GetStudentAttendanceTrendAsync(Guid studentId, AttendancePeriodType periodType);

        Task<AttendanceReportDto> GetClassAttendanceReportAsync(Guid classId, AttendancePeriodType periodType, DateTime? startDate, DateTime? endDate);
        Task<AttendanceSummaryDto> GetClassAttendanceSummaryAsync(Guid classId, AttendancePeriodType periodType, DateTime? startDate, DateTime? endDate);
        Task<IEnumerable<AttendanceResponse>> GetClassAttendanceDetailedListAsync(Guid classId, AttendancePeriodType periodType, DateTime? startDate, DateTime? endDate);
        Task<decimal> GetClassAttendancePercentageAsync(Guid classId, AttendancePeriodType periodType);
        Task<int> GetClassPresentCountAsync(Guid classId, AttendancePeriodType periodType);
        Task<int> GetClassAbsentCountAsync(Guid classId, AttendancePeriodType periodType);
        Task<int> GetClassLeaveCountAsync(Guid classId, AttendancePeriodType periodType);
        Task<int> GetClassHalfDayCountAsync(Guid classId, AttendancePeriodType periodType);
        Task<IEnumerable<ClassStudentAttendanceSummaryDto>> GetClassLowAttendanceStudentsAsync(Guid classId, decimal threshold, AttendancePeriodType periodType);
        Task<IEnumerable<ClassStudentAttendanceSummaryDto>> GetClassChronicAbsenteesAsync(Guid classId, int consecutiveDays);
        Task<IEnumerable<AttendanceTrendPointDto>> GetClassAttendanceTrendAsync(Guid classId, AttendancePeriodType periodType);
        Task<IEnumerable<MonthAttendanceComparisonDto>> CompareClassAttendanceAcrossMonthsAsync(Guid classId);
        Task<DropoutRiskDto> PredictDropoutRiskForClassAsync(Guid classId);
        Task<AttendanceVsResultDto> AttendanceVsResultAnalysisForClassAsync(Guid classId);
        Task<IEnumerable<ClassStudentAttendanceSummaryDto>> GetTopRegularStudentsInClassAsync(Guid classId);
        Task<ClassAttendanceHeatmapDto> GetClassAttendanceHeatmapAsync(Guid classId);
    }
}
