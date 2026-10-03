using Application.ViewModels.Student;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface IStudentService
    {
        Task<IEnumerable<StudentResponse>> GetAllAsync();
        Task<PagedStudentsResponse> GetPagedAsync(int page, int pageSize);
        Task<StudentResponse?> GetByIdAsync(Guid id);
        Task<StudentResponse> CreateAsync(StudentRequest request);
        Task<StudentResponse?> UpdateAsync(Guid id, StudentRequest request);
        Task<StudentResponse?> PatchAsync(Guid id, StudentRequest dto);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> SoftDeleteAsync(Guid id);
        Task<bool> RestoreAsync(Guid id);

        // Lifecycle / status methods
        Task<StudentResponse?> ActivateStudentAsync(Guid id);
        Task<StudentResponse?> SuspendStudentAsync(Guid id);
        Task<StudentResponse?> DropStudentAsync(Guid id);
        Task<StudentResponse?> TransferStudentAsync(Guid id, Guid newBranchId);
        Task<StudentResponse?> PromoteStudentAsync(Guid id, Guid newClassId, Guid academicYearId);
        Task<StudentResponse?> MarkStudentPassedOutAsync(Guid id);
        Task<StudentResponse?> IssueTcAsync(Guid id);
        Task<StudentResponse?> ReEnrollStudentAsync(Guid id);
        Task<StudentResponse?> UpdateStudentStatusAsync(Guid id, string status);
        Task<StudentResponse?> UpdateLeavingDateAsync(Guid id, DateOnly date);

        // Additional query methods
        Task<IEnumerable<StudentResponse>> GetByDivisionIdAsync(Guid divisionId);
        Task<IEnumerable<StudentResponse>> GetByClassIdAsync(Guid classId);
        Task<IEnumerable<StudentResponse>> GetByBranchIdAsync(Guid branchId);
        Task<IEnumerable<StudentResponse>> GetByClassTeacherIdAsync(Guid staffId);
        Task<IEnumerable<StudentResponse>> GetByAcademicYearIdAsync(Guid academicYearId);
        Task<IEnumerable<StudentResponse>> GetByPreviousSchoolAsync(string schoolName);
        Task<IEnumerable<StudentResponse>> GetByPreviousBoardAsync(string boardName);
        Task<StudentResponse?> GetByAadhaarAsync(string aadhaar);
        Task<StudentResponse?> GetByAdmissionNumberAsync(Guid branchId, string admissionNo);
        Task<StudentResponse?> GetByRollNumberAsync(Guid classId, string rollNo);
        Task<IEnumerable<StudentResponse>> GetByFatherPhoneAsync(string phone);
        Task<IEnumerable<StudentResponse>> GetByMotherPhoneAsync(string phone);
        Task<IEnumerable<StudentResponse>> GetByGuardianPhoneAsync(string phone);
        Task<StudentResponse?> GetByEmailAsync(string email);
        Task<IEnumerable<StudentResponse>> GetByGenderAsync(string gender);
        Task<IEnumerable<StudentResponse>> GetByCasteAsync(string caste);
        Task<IEnumerable<StudentResponse>> GetByCategoryAsync(string category);
        Task<IEnumerable<StudentResponse>> GetByReligionAsync(string religion);
        Task<IEnumerable<StudentResponse>> GetByNationalityAsync(string nationality);
        Task<IEnumerable<StudentResponse>> GetByBloodGroupAsync(string bloodGroup);
        Task<IEnumerable<StudentResponse>> GetByDisabilityAsync(string disability);
        Task<IEnumerable<StudentResponse>> GetByCityAsync(string city);
        Task<IEnumerable<StudentResponse>> GetByStateAsync(string state);
        Task<IEnumerable<StudentResponse>> GetByPincodeAsync(string pincode);
        Task<IEnumerable<StudentResponse>> GetByAddressKeywordAsync(string keyword);
        Task<IEnumerable<StudentResponse>> GetByFamilyIncomeRangeAsync(decimal min, decimal max);
        Task<IEnumerable<StudentResponse>> GetWithOutstandingFeesAsync();
        Task<IEnumerable<StudentResponse>> GetWithScholarshipAsync();
        Task<IEnumerable<StudentResponse>> GetWithMedicalConditionAsync();
        Task<IEnumerable<StudentResponse>> GetWithAllergiesAsync();
        Task<IEnumerable<StudentResponse>> GetByHeightRangeAsync(decimal min, decimal max);
        Task<IEnumerable<StudentResponse>> GetByWeightRangeAsync(decimal min, decimal max);
        Task<IEnumerable<StudentResponse>> GetByDobRangeAsync(DateOnly fromDate, DateOnly toDate);
        Task<IEnumerable<StudentResponse>> GetByAdmissionDateRangeAsync(DateOnly fromDate, DateOnly toDate);
        Task<IEnumerable<StudentResponse>> GetByLeavingDateRangeAsync(DateOnly fromDate, DateOnly toDate);
        Task<IEnumerable<StudentResponse>> GetByStatusAsync(string status);
        Task<IEnumerable<StudentResponse>> GetActiveStudentsAsync();
        Task<IEnumerable<StudentResponse>> GetInactiveStudentsAsync();
        Task<IEnumerable<StudentResponse>> GetDroppedStudentsAsync();
        Task<IEnumerable<StudentResponse>> GetTransferredStudentsAsync();
        Task<IEnumerable<StudentResponse>> GetPassedOutStudentsAsync();
        Task<IEnumerable<StudentResponse>> SearchStudentsAsync(StudentFilterDto filterDto);
        Task<IEnumerable<StudentResponse>> AdvancedStudentSearchAsync(AdvancedStudentSearchDto multiCriteriaDto);
        Task<IEnumerable<StudentResponse>> GlobalStudentSearchAsync(string keyword);
        Task<IEnumerable<StudentStrengthItemDto>> GetStudentStrengthByClassAsync(Guid branchId);
        Task<IEnumerable<StudentStrengthItemDto>> GetStudentStrengthByDivisionAsync(Guid branchId);
        Task<IEnumerable<DistributionItemDto>> GetGenderDistributionAsync(Guid branchId);
        Task<IEnumerable<DistributionItemDto>> GetCategoryWiseDistributionAsync(Guid branchId);
        Task<IEnumerable<DistributionItemDto>> GetCasteWiseDistributionAsync(Guid branchId);
        Task<IEnumerable<DistributionItemDto>> GetReligionWiseDistributionAsync(Guid branchId);
        Task<IEnumerable<DistributionItemDto>> GetCityWiseDistributionAsync(Guid branchId);
        Task<IEnumerable<DistributionItemDto>> GetStateWiseDistributionAsync(Guid branchId);
        Task<IEnumerable<DistributionItemDto>> GetBloodGroupDistributionAsync(Guid branchId);
        Task<IEnumerable<DistributionItemDto>> GetAgeWiseDistributionAsync(Guid branchId);
        Task<IEnumerable<DistributionItemDto>> GetIncomeDistributionAsync(Guid branchId);
        Task<IEnumerable<StudentStrengthItemDto>> GetStudentGrowthTrendAsync(Guid branchId);
        Task<IEnumerable<YearCountDto>> GetYearWiseAdmissionTrendAsync(Guid branchId);
        Task<RateAnalysisDto> DropoutRateAnalysisAsync(Guid branchId);
        Task<RateAnalysisDto> PromotionSuccessRateAsync(Guid academicYearId);
        Task<RateAnalysisDto> StudentRetentionRateAsync();
        Task<RateAnalysisDto> TransferRateAnalysisAsync(Guid branchId);
        Task<IEnumerable<GenderRatioTrendPointDto>> GetGenderRatioTrendAsync(Guid branchId);
        Task<IEnumerable<CasteTrendPointDto>> GetCasteTrendAnalysisAsync(Guid branchId);
        Task<IEnumerable<DistributionItemDto>> UrbanRuralDistributionAnalysisAsync(Guid branchId);
        Task<RateAnalysisDto> AdmissionConversionRateAsync();
        Task<StudentReportDto> GenerateUDISEReportAsync();
        Task<StudentReportDto> GenerateMinorityReportAsync();
        Task<StudentReportDto> GenerateSCSTOBCReportAsync();
        Task<StudentReportDto> GenerateAadhaarLinkedStudentsReportAsync();
        Task<StudentReportDto> GenerateScholarshipEligibleStudentsReportAsync();
        Task<StudentReportDto> GenerateRTEEligibleStudentsReportAsync();
        Task<int> GetTotalStudentsAsync();
        Task<IEnumerable<StudentResponse>> GetTodayAdmissionsAsync();
        Task<IEnumerable<StudentResponse>> GetStudentsWithBirthdayTodayAsync();
        Task<IEnumerable<StudentResponse>> GetStudentsWithPendingDocumentsAsync();
        Task<IEnumerable<StudentResponse>> GetStudentsWithLowAttendanceAsync(decimal minAttendancePercent = 75);
        Task<IEnumerable<StudentResponse>> GetStudentsEligibleForBoardExamAsync();

        // Student summary / helper methods
        Task<StudentProfileSummaryDto?> GetStudentProfileSummaryAsync(Guid studentId);
        Task<StudentAcademicSummaryDto?> GetStudentAcademicSummaryAsync(Guid studentId);
        Task<StudentFeeSummaryDto?> GetStudentFeeSummaryAsync(Guid studentId);
        Task<StudentAttendanceSummaryDto?> GetStudentAttendanceSummaryAsync(Guid studentId);
        Task<StudentAgeDto?> CalculateStudentAgeAsync(Guid studentId);
        Task<StudentAcademicDurationDto?> CalculateAcademicDurationAsync(Guid studentId);
        Task<string?> GetFullNameAsync(Guid studentId);

        // Bulk upload (Excel template + import; uses NPOI open source)
        Task<byte[]> GetStudentBulkUploadTemplateAsync();
        Task<StudentBulkUploadResultDto> BulkUploadStudentsAsync(Stream excelStream);

        // Validation / check methods
        Task<bool> CheckAdmissionNumberExistsAsync(Guid branchId, string admissionNo);
        Task<bool> CheckRollNumberExistsAsync(Guid classId, string rollNo);
        Task<bool> CheckAadhaarExistsAsync(string childAadhaar);
        Task<ValidationResultResponse> ValidateAadhaarAsync(string childAadhaar);
        Task<ValidationResultResponse> ValidateAgeEligibilityAsync(DateOnly dob, Guid? classId);
        Task<ValidationResultResponse> ValidateDuplicateStudentAsync(StudentRequest dto);
        Task<ValidationResultResponse> ValidatePromotionEligibilityAsync(Guid studentId);
        Task<ValidationResultResponse> ValidateTransferEligibilityAsync(Guid studentId);
    }
}