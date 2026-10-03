using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.ViewModels.Fees;

namespace Application.Abstractions.Services;

public interface IFeeStudentService
{
    Task<StudentFeeDashboardDto> GetStudentDashboard(Guid studentId, Guid academicYearId);
    Task<StudentFeeDashboardDto> GetStudentDashboardByRoll(Guid branchId, Guid classId, string rollNumber, Guid academicYearId);
}

public interface IFeeAnalyticsService
{
    Task<ClassFeeDashboardDto> GetClassDashboard(Guid branchId, Guid classId, Guid academicYearId);
    Task<DivisionFeeDashboardDto> GetDivisionDashboard(Guid branchId, Guid divisionId, Guid academicYearId);
    Task<BranchFeeDashboardDto> GetBranchDashboard(Guid branchId, Guid academicYearId);

    Task<List<FeeCollectionTrendDto>> GetCollectionTrend(Guid branchId, DateTime startDate, DateTime endDate);
    Task<List<FeeHeadAnalyticsDto>> GetFeeHeadAnalytics(Guid branchId, Guid academicYearId, Guid? classId, Guid? divisionId);
    Task<List<InstallmentCollectionDto>> GetInstallmentAnalytics(Guid branchId, Guid academicYearId, Guid? classId, Guid? divisionId);

    Task<List<StudentOutstandingFeeDto>> GetOutstandingByStudent(Guid branchId, Guid? classId, Guid? divisionId, Guid academicYearId);
    Task<List<ClassOutstandingFeeDto>> GetOutstandingByClass(Guid branchId, Guid academicYearId);
    Task<List<DivisionOutstandingFeeDto>> GetOutstandingByDivision(Guid branchId, Guid academicYearId);
    Task<List<BranchOutstandingFeeDto>> GetOutstandingByBranch(Guid academicYearId);

    Task<List<StudentFeeHeadStatusDto>> GetFeeHeadStatus(
        Guid feeHeadId,
        Guid branchId,
        Guid academicYearId,
        Guid? classId,
        Guid? divisionId);
}

