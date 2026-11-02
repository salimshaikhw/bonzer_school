using Application.Attendances;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services.Academics;
using Application.Abstractions.Services.Students;
using Application.Abstractions.Services;
using Application.Abstractions;
using Application.Common.Helpers;
using Application.Common;
using Application.Exceptions;
using Application.Students;
using Application.Validation.Common;
using Application.ViewModels.Student;
using AutoMapper;
using Infrastructure.Services;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace Infrastructure.Services.Students
{
    /// <summary>Statutory student reports (UDISE, RTE, ...) and per-student summaries (read only).</summary>
    public class StudentReportService : StudentServiceBase, IStudentReportService
    {
        public StudentReportService(StudentServiceDependencies deps, ILogger<StudentReportService> logger)
            : base(deps, logger)
        {
        }


        public async Task<StudentReportDto> GenerateUDISEReportAsync()
        {
            try
            {
                _logger.LogInformation("GenerateUDISEReport");
                const string cacheKey = "students:report:udise";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning UDISE report from cache");
                    return _cacheService.Get<StudentReportDto>(cacheKey);
                }
                var students = await _studentRepository.GetAllAsync();
                var list = students.ToList();
                var byGender = list.GroupBy(s => s.Gender?.Trim() ?? "Not specified").Select(g => new DistributionItemDto { Key = g.Key, Count = g.Count() }).ToList();
                var byCategory = list.GroupBy(s => s.Category?.Trim() ?? "Not specified").Select(g => new DistributionItemDto { Key = g.Key, Count = g.Count() }).ToList();
                var byClass = list.Where(s => s.ClassId.HasValue).GroupBy(s => s.ClassId!.Value).Select(g => new DistributionItemDto { Key = g.Key.ToString(), Count = g.Count() }).ToList();
                var report = new StudentReportDto
                {
                    ReportTitle = "UDISE Report",
                    GeneratedOn = DateTime.UtcNow,
                    TotalCount = list.Count,
                    Description = "Unified District Information System for Education - student summary.",
                    Sections = new List<ReportSectionDto>
                    {
                        new ReportSectionDto { Title = "By Gender", Items = byGender },
                        new ReportSectionDto { Title = "By Category", Items = byCategory },
                        new ReportSectionDto { Title = "By Class", Items = byClass }
                    }
                };
                _cacheService.Set(cacheKey, report);
                return report;
            }
            catch (Exception ex)
            {
                throw new Application.Exceptions.BaseServiceException("Error generating UDISE report", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentReportDto> GenerateMinorityReportAsync()
        {
            try
            {
                _logger.LogInformation("GenerateMinorityReport");
                const string cacheKey = "students:report:minority";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning minority report from cache");
                    return _cacheService.Get<StudentReportDto>(cacheKey);
                }
                var minorityReligions = new[] { "MUSLIM", "CHRISTIAN", "SIKH", "BUDDHIST", "JAIN", "PARSI", "OTHER" };
                var students = await _studentRepository.GetAllAsync();
                var list = students.Where(s => s.Religion != null && minorityReligions.Contains(s.Religion.Trim().ToUpperInvariant())).ToList();
                var byReligion = list.GroupBy(s => s.Religion?.Trim() ?? "Not specified").Select(g => new DistributionItemDto { Key = g.Key!, Count = g.Count() }).ToList();
                var report = new StudentReportDto
                {
                    ReportTitle = "Minority Students Report",
                    GeneratedOn = DateTime.UtcNow,
                    TotalCount = list.Count,
                    Description = "Students belonging to minority religions (Muslim, Christian, Sikh, Buddhist, Jain, Parsi, Other).",
                    Sections = new List<ReportSectionDto> { new ReportSectionDto { Title = "By Religion", Items = byReligion } },
                    Students = list.Select(MapStudentToResponse).ToList()
                };
                _cacheService.Set(cacheKey, report);
                return report;
            }
            catch (Exception ex)
            {
                throw new Application.Exceptions.BaseServiceException("Error generating minority report", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentReportDto> GenerateSCSTOBCReportAsync()
        {
            try
            {
                _logger.LogInformation("GenerateSCSTOBCReport");
                const string cacheKey = "students:report:scstobc";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning SC/ST/OBC report from cache");
                    return _cacheService.Get<StudentReportDto>(cacheKey);
                }
                var students = await _studentRepository.GetAllAsync();
                var list = students.Where(s => s.Category != null && new[] { "SC", "ST", "OBC" }.Contains(s.Category.Trim().ToUpperInvariant())).ToList();
                var byCategory = list.GroupBy(s => s.Category?.Trim() ?? "Not specified").Select(g => new DistributionItemDto { Key = g.Key!, Count = g.Count() }).ToList();
                var report = new StudentReportDto
                {
                    ReportTitle = "SC/ST/OBC Students Report",
                    GeneratedOn = DateTime.UtcNow,
                    TotalCount = list.Count,
                    Description = "Scheduled Caste, Scheduled Tribe and Other Backward Class students.",
                    Sections = new List<ReportSectionDto> { new ReportSectionDto { Title = "By Category", Items = byCategory } },
                    Students = list.Select(MapStudentToResponse).ToList()
                };
                _cacheService.Set(cacheKey, report);
                return report;
            }
            catch (Exception ex)
            {
                throw new Application.Exceptions.BaseServiceException("Error generating SC/ST/OBC report", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentReportDto> GenerateAadhaarLinkedStudentsReportAsync()
        {
            try
            {
                _logger.LogInformation("GenerateAadhaarLinkedStudentsReport");
                const string cacheKey = "students:report:aadhaarlinked";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning Aadhaar linked report from cache");
                    return _cacheService.Get<StudentReportDto>(cacheKey);
                }
                var students = await _studentRepository.GetAllAsync();
                var list = students.Where(s => !string.IsNullOrWhiteSpace(s.ChildAadhaar)).ToList();
                var verified = list.Count(s => s.IsAadhaarVerified == true);
                var notVerified = list.Count - verified;
                var report = new StudentReportDto
                {
                    ReportTitle = "Aadhaar Linked Students Report",
                    GeneratedOn = DateTime.UtcNow,
                    TotalCount = list.Count,
                    Description = "Students with Child Aadhaar linked; breakdown by verification status.",
                    Sections = new List<ReportSectionDto>
                    {
                        new ReportSectionDto { Title = "Verification Status", Items = new List<DistributionItemDto> { new DistributionItemDto { Key = "Verified", Count = verified }, new DistributionItemDto { Key = "Not Verified", Count = notVerified } } }
                    },
                    Students = list.Select(MapStudentToResponse).ToList()
                };
                _cacheService.Set(cacheKey, report);
                return report;
            }
            catch (Exception ex)
            {
                throw new Application.Exceptions.BaseServiceException("Error generating Aadhaar linked report", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentReportDto> GenerateScholarshipEligibleStudentsReportAsync()
        {
            try
            {
                _logger.LogInformation("GenerateScholarshipEligibleStudentsReport");
                const string cacheKey = "students:report:scholarshipeligible";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning scholarship eligible report from cache");
                    return _cacheService.Get<StudentReportDto>(cacheKey);
                }
                const decimal incomeThreshold = 200000;
                var students = await _studentRepository.GetAllAsync();
                var list = students.Where(s =>
                    (s.FamilyIncome.HasValue && s.FamilyIncome.Value <= incomeThreshold) ||
                    (s.Category != null && new[] { "SC", "ST", "OBC" }.Contains(s.Category.Trim().ToUpperInvariant()))).ToList();
                var byEligibility = new List<DistributionItemDto>();
                byEligibility.Add(new DistributionItemDto { Key = "Family income ≤ 2L", Count = students.Count(s => s.FamilyIncome.HasValue && s.FamilyIncome.Value <= incomeThreshold) });
                byEligibility.Add(new DistributionItemDto { Key = "SC/ST/OBC", Count = students.Count(s => s.Category != null && new[] { "SC", "ST", "OBC" }.Contains(s.Category.Trim().ToUpperInvariant())) });
                var report = new StudentReportDto
                {
                    ReportTitle = "Scholarship Eligible Students Report",
                    GeneratedOn = DateTime.UtcNow,
                    TotalCount = list.Count,
                    Description = "Students eligible by family income (≤2L) or SC/ST/OBC category. Eligibility criteria may vary by scheme.",
                    Sections = new List<ReportSectionDto> { new ReportSectionDto { Title = "Eligibility Criteria", Items = byEligibility } },
                    Students = list.Select(MapStudentToResponse).ToList()
                };
                _cacheService.Set(cacheKey, report);
                return report;
            }
            catch (Exception ex)
            {
                throw new Application.Exceptions.BaseServiceException("Error generating scholarship eligible report", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentReportDto> GenerateRTEEligibleStudentsReportAsync()
        {
            try
            {
                _logger.LogInformation("GenerateRTEEligibleStudentsReport");
                const string cacheKey = "students:report:rteeligible";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning RTE eligible report from cache");
                    return _cacheService.Get<StudentReportDto>(cacheKey);
                }
                const decimal incomeThreshold = 200000;
                var students = await _studentRepository.GetAllAsync();
                var list = students.Where(s =>
                    (s.FamilyIncome.HasValue && s.FamilyIncome.Value <= incomeThreshold) ||
                    (s.Category != null && new[] { "SC", "ST", "OBC", "EWS", "DG" }.Contains(s.Category.Trim().ToUpperInvariant()))).ToList();
                var byEligibility = new List<DistributionItemDto>();
                byEligibility.Add(new DistributionItemDto { Key = "Family income ≤ 2L", Count = students.Count(s => s.FamilyIncome.HasValue && s.FamilyIncome.Value <= incomeThreshold) });
                byEligibility.Add(new DistributionItemDto { Key = "SC/ST/OBC/EWS/DG", Count = students.Count(s => s.Category != null && new[] { "SC", "ST", "OBC", "EWS", "DG" }.Contains(s.Category.Trim().ToUpperInvariant())) });
                var report = new StudentReportDto
                {
                    ReportTitle = "RTE Eligible Students Report",
                    GeneratedOn = DateTime.UtcNow,
                    TotalCount = list.Count,
                    Description = "Students eligible under RTE (Right to Education) by income (≤2L) or category (SC/ST/OBC/EWS/DG).",
                    Sections = new List<ReportSectionDto> { new ReportSectionDto { Title = "Eligibility Criteria", Items = byEligibility } },
                    Students = list.Select(MapStudentToResponse).ToList()
                };
                _cacheService.Set(cacheKey, report);
                return report;
            }
            catch (Exception ex)
            {
                throw new Application.Exceptions.BaseServiceException("Error generating RTE eligible report", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentProfileSummaryDto?> GetStudentProfileSummaryAsync(Guid studentId)
        {
            try
            {
                _logger.LogInformation("GetStudentProfileSummary: {StudentId}", studentId);
                var student = await _studentRepository.GetByIdAsync(studentId);
                if (student == null) return null;
                var (years, months) = student.Dob.HasValue ? GetAgeFromDob(student.Dob.Value) : (null, null);
                return new StudentProfileSummaryDto
                {
                    StudentId = student.Id,
                    FullName = BuildFullName(student.FirstName, student.MiddleName, student.LastName),
                    AdmissionNo = student.AdmissionNo,
                    RollNumber = student.RollNumber,
                    Gender = student.Gender,
                    Dob = student.Dob,
                    AgeYears = years,
                    BloodGroup = student.BloodGroup,
                    Category = student.Category,
                    Religion = student.Religion,
                    Phone = student.Phone,
                    Email = student.Email,
                    StudentStatus = student.StudentStatus,
                    IsActive = student.IsActive,
                    PhotoUrl = student.PhotoUrl
                };
            }
            catch (Exception ex)
            {
                throw new Application.Exceptions.BaseServiceException("Error getting student profile summary", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentAcademicSummaryDto?> GetStudentAcademicSummaryAsync(Guid studentId)
        {
            try
            {
                _logger.LogInformation("GetStudentAcademicSummary: {StudentId}", studentId);
                var student = await _studentRepository.GetByIdAsync(studentId);
                if (student == null) return null;
                string? className = null, divisionName = null, branchName = null, academicYearName = null;
                if (student.ClassId.HasValue)
                {
                    var c = await _classRepository.GetByIdAsync(student.ClassId.Value);
                    className = c?.Name;
                }
                if (student.DivisionId.HasValue)
                {
                    var d = await _divisionRepository.GetByIdAsync(student.DivisionId.Value);
                    divisionName = d?.Name;
                }
                if (student.BranchId.HasValue)
                {
                    var b = await _branchRepository.GetByIdAsync(student.BranchId.Value);
                    branchName = b?.Name;
                }
                if (student.AcademicYearId.HasValue)
                {
                    var y = await _academicYearRepository.GetByIdAsync(student.AcademicYearId.Value);
                    academicYearName = y?.YearName;
                }
                return new StudentAcademicSummaryDto
                {
                    StudentId = student.Id,
                    ClassName = className,
                    ClassId = student.ClassId,
                    DivisionName = divisionName,
                    DivisionId = student.DivisionId,
                    BranchName = branchName,
                    BranchId = student.BranchId,
                    AcademicYearName = academicYearName,
                    AcademicYearId = student.AcademicYearId,
                    AdmissionDate = student.AdmissionDate,
                    RollNumber = student.RollNumber,
                    EnrollmentNo = student.EnrollmentNo,
                    LastExamName = student.LastExamName,
                    LastExamResult = student.LastExamResult
                };
            }
            catch (Exception ex)
            {
                throw new Application.Exceptions.BaseServiceException("Error getting student academic summary", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentFeeSummaryDto?> GetStudentFeeSummaryAsync(Guid studentId)
        {
            try
            {
                _logger.LogInformation("GetStudentFeeSummary: {StudentId}", studentId);
                var student = await _studentRepository.GetByIdAsync(studentId);
                if (student == null) return null;
                var invoices = await _invoiceRepository.FindAllAsync(i => i.StudentId == studentId);
                var list = invoices ?? new List<Invoice>();
                var totalBilled = list.Sum(i => i.TotalAmount ?? 0);
                var totalPaid = list.Sum(i => i.PaidAmount ?? 0);
                var totalOutstanding = list.Sum(i => i.RemainingAmount ?? 0);
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var overdue = list.Count(i => i.DueDate.HasValue && i.DueDate.Value < today && (i.RemainingAmount ?? 0) > 0);
                return new StudentFeeSummaryDto
                {
                    StudentId = studentId,
                    TotalInvoices = list.Count,
                    TotalBilled = totalBilled,
                    TotalPaid = totalPaid,
                    TotalOutstanding = totalOutstanding,
                    InvoicesOverdue = overdue
                };
            }
            catch (Exception ex)
            {
                throw new Application.Exceptions.BaseServiceException("Error getting student fee summary", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentAttendanceSummaryDto?> GetStudentAttendanceSummaryAsync(Guid studentId)
        {
            try
            {
                _logger.LogInformation("GetStudentAttendanceSummary: {StudentId}", studentId);
                var student = await _studentRepository.GetByIdAsync(studentId);
                if (student == null) return null;
                var now = DateTime.UtcNow;
                var startOfMonth = new DateOnly(now.Year, now.Month, 1);
                var today = DateOnly.FromDateTime(now);
                var records = await _attendanceRepository.FindAllAsync(a =>
                    a.StudentId == studentId &&
                    a.AttendanceDate.HasValue &&
                    a.AttendanceDate.Value >= startOfMonth &&
                    a.AttendanceDate.Value <= today);
                var list = records ?? new List<Attendance>();
                // Holidays are not working days; the percentage follows the shared AttendanceTally rule.
                var tally = AttendanceTally.Of(list.Where(a => a.IsHoliday != true).Select(a => a.Status));
                var totalDays = tally.WorkingDays;
                var present = tally.Present + tally.Late;
                var absent = tally.Absent;
                var leave = tally.Leave;
                var percent = tally.Percentage;
                return new StudentAttendanceSummaryDto
                {
                    StudentId = studentId,
                    PeriodStart = startOfMonth,
                    PeriodEnd = today,
                    TotalDays = totalDays,
                    PresentDays = present,
                    AbsentDays = absent,
                    LeaveDays = leave,
                    AttendancePercent = Math.Round(percent, 2)
                };
            }
            catch (Exception ex)
            {
                throw new Application.Exceptions.BaseServiceException("Error getting student attendance summary", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentAgeDto?> CalculateStudentAgeAsync(Guid studentId)
        {
            try
            {
                _logger.LogInformation("CalculateStudentAge: {StudentId}", studentId);
                var student = await _studentRepository.GetByIdAsync(studentId);
                if (student == null || !student.Dob.HasValue) return null;
                var (years, months) = GetAgeFromDob(student.Dob.Value);
                var display = years.HasValue && months.HasValue ? $"{years} years, {months} months" : null;
                return new StudentAgeDto
                {
                    StudentId = studentId,
                    Dob = student.Dob,
                    Years = years,
                    Months = months,
                    Display = display
                };
            }
            catch (Exception ex)
            {
                throw new Application.Exceptions.BaseServiceException("Error calculating student age", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentAcademicDurationDto?> CalculateAcademicDurationAsync(Guid studentId)
        {
            try
            {
                _logger.LogInformation("CalculateAcademicDuration: {StudentId}", studentId);
                var student = await _studentRepository.GetByIdAsync(studentId);
                if (student == null || !student.AdmissionDate.HasValue) return null;
                var (years, months) = GetDurationBetween(student.AdmissionDate.Value, DateOnly.FromDateTime(DateTime.UtcNow));
                var display = years.HasValue && months.HasValue ? $"{years} years, {months} months" : null;
                return new StudentAcademicDurationDto
                {
                    StudentId = studentId,
                    AdmissionDate = student.AdmissionDate,
                    Years = years,
                    Months = months,
                    Display = display
                };
            }
            catch (Exception ex)
            {
                throw new Application.Exceptions.BaseServiceException("Error calculating academic duration", ex, Application.Common.ExceptionCodes.Database);
            }
        }
    }
}
