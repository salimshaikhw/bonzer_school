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
    /// <summary>Student distributions, trends and rates per branch (read only).</summary>
    public class StudentAnalyticsService : StudentServiceBase, IStudentAnalyticsService
    {
        public StudentAnalyticsService(StudentServiceDependencies deps, ILogger<StudentAnalyticsService> logger)
            : base(deps, logger)
        {
        }


        public async Task<IEnumerable<StudentStrengthItemDto>> GetStudentStrengthByClassAsync(Guid branchId)
        {
            return await GetStrengthByBranchAsync(branchId, "class", s => s.ClassId);
        }

        public async Task<IEnumerable<StudentStrengthItemDto>> GetStudentStrengthByDivisionAsync(Guid branchId)
        {
            return await GetStrengthByBranchAsync(branchId, "division", s => s.DivisionId);
        }

        private async Task<IEnumerable<StudentStrengthItemDto>> GetStrengthByBranchAsync(Guid branchId, string suffix, System.Linq.Expressions.Expression<System.Func<Student, Guid?>> groupSelector)
        {
            try
            {
                _logger.LogInformation("GetStudentStrengthBy{Suffix}: BranchId {BranchId}", suffix, branchId);
                var cacheKey = $"students:strength:{suffix}:{branchId}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning strength by {Suffix} from cache", suffix);
                    return _cacheService.Get<IEnumerable<StudentStrengthItemDto>>(cacheKey);
                }
                var query = _studentRepository.Query(s => s.BranchId == branchId);
                var grouped = await query.GroupBy(groupSelector).Select(g => new StudentStrengthItemDto { Id = g.Key, Count = g.Count() }).ToListAsync();
                _cacheService.Set(cacheKey, grouped);
                return grouped;
            }
            catch (Exception ex)
            {
                throw new Application.Exceptions.BaseServiceException($"Error getting student strength by {suffix}", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<DistributionItemDto>> GetGenderDistributionAsync(Guid branchId)
        {
            return await GetDistributionByBranchAsync(branchId, "gender", s => s.Gender);
        }

        public async Task<IEnumerable<DistributionItemDto>> GetCategoryWiseDistributionAsync(Guid branchId)
        {
            return await GetDistributionByBranchAsync(branchId, "category", s => s.Category);
        }

        public async Task<IEnumerable<DistributionItemDto>> GetCasteWiseDistributionAsync(Guid branchId)
        {
            return await GetDistributionByBranchAsync(branchId, "caste", s => s.Caste);
        }

        public async Task<IEnumerable<DistributionItemDto>> GetReligionWiseDistributionAsync(Guid branchId)
        {
            return await GetDistributionByBranchAsync(branchId, "religion", s => s.Religion);
        }

        public async Task<IEnumerable<DistributionItemDto>> GetCityWiseDistributionAsync(Guid branchId)
        {
            return await GetDistributionByBranchAsync(branchId, "city", s => s.City);
        }

        public async Task<IEnumerable<DistributionItemDto>> GetStateWiseDistributionAsync(Guid branchId)
        {
            return await GetDistributionByBranchAsync(branchId, "state", s => s.State);
        }

        public async Task<IEnumerable<DistributionItemDto>> GetBloodGroupDistributionAsync(Guid branchId)
        {
            return await GetDistributionByBranchAsync(branchId, "bloodgroup", s => s.BloodGroup);
        }

        private async Task<IEnumerable<DistributionItemDto>> GetDistributionByBranchAsync(Guid branchId, string suffix, System.Linq.Expressions.Expression<System.Func<Student, string?>> groupSelector)
        {
            try
            {
                _logger.LogInformation("Get{Suffix}Distribution: BranchId {BranchId}", suffix, branchId);
                var cacheKey = $"students:dist:{suffix}:{branchId}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning {Suffix} distribution from cache", suffix);
                    return _cacheService.Get<IEnumerable<DistributionItemDto>>(cacheKey);
                }
                var query = _studentRepository.Query(s => s.BranchId == branchId);
                var grouped = await query.GroupBy(groupSelector).Select(g => new DistributionItemDto { Key = g.Key ?? "Not specified", Count = g.Count() }).ToListAsync();
                _cacheService.Set(cacheKey, grouped);
                return grouped;
            }
            catch (Exception ex)
            {
                throw new Application.Exceptions.BaseServiceException($"Error getting {suffix} distribution", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<DistributionItemDto>> GetAgeWiseDistributionAsync(Guid branchId)
        {
            try
            {
                _logger.LogInformation("GetAgeWiseDistribution: BranchId {BranchId}", branchId);
                var cacheKey = $"students:dist:age:{branchId}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning age-wise distribution from cache");
                    return _cacheService.Get<IEnumerable<DistributionItemDto>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s => s.BranchId == branchId && s.Dob.HasValue);
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var buckets = new[] { "0-5", "6-10", "11-15", "16-18", "19+" };
                var grouped = students
                    .Select(s => new { Age = today.Year - s.Dob!.Value.Year - (today.DayOfYear < s.Dob.Value.DayOfYear ? 1 : 0) })
                    .GroupBy(x => x.Age switch { <= 5 => "0-5", <= 10 => "6-10", <= 15 => "11-15", <= 18 => "16-18", _ => "19+" })
                    .Select(g => new DistributionItemDto { Key = g.Key, Count = g.Count() })
                    .OrderBy(d => Array.IndexOf(buckets, d.Key))
                    .ToList();
                _cacheService.Set(cacheKey, grouped);
                return grouped;
            }
            catch (Exception ex)
            {
                throw new Application.Exceptions.BaseServiceException("Error getting age-wise distribution", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<DistributionItemDto>> GetIncomeDistributionAsync(Guid branchId)
        {
            try
            {
                _logger.LogInformation("GetIncomeDistribution: BranchId {BranchId}", branchId);
                var cacheKey = $"students:dist:income:{branchId}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning income distribution from cache");
                    return _cacheService.Get<IEnumerable<DistributionItemDto>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s => s.BranchId == branchId);
                var buckets = new[] { "0-50K", "50K-1L", "1L-2L", "2L-5L", "5L+" };
                var grouped = students
                    .Select(s => s.FamilyIncome)
                    .GroupBy(inc => inc == null ? "Not specified" : inc.Value switch { <= 50000 => "0-50K", <= 100000 => "50K-1L", <= 200000 => "1L-2L", <= 500000 => "2L-5L", _ => "5L+" })
                    .Select(g => new DistributionItemDto { Key = g.Key, Count = g.Count() })
                    .OrderBy(d => d.Key == "Not specified" ? -1 : Array.IndexOf(buckets, d.Key))
                    .ToList();
                _cacheService.Set(cacheKey, grouped);
                return grouped;
            }
            catch (Exception ex)
            {
                throw new Application.Exceptions.BaseServiceException("Error getting income distribution", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentStrengthItemDto>> GetStudentGrowthTrendAsync(Guid branchId)
        {
            try
            {
                _logger.LogInformation("GetStudentGrowthTrend: BranchId {BranchId}", branchId);
                var cacheKey = $"students:trend:growth:{branchId}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning student growth trend from cache");
                    return _cacheService.Get<IEnumerable<StudentStrengthItemDto>>(cacheKey);
                }
                var query = _studentRepository.Query(s => s.BranchId == branchId);
                var grouped = await query.GroupBy(s => s.AcademicYearId).Select(g => new StudentStrengthItemDto { Id = g.Key, Count = g.Count() }).ToListAsync();
                _cacheService.Set(cacheKey, grouped);
                return grouped;
            }
            catch (Exception ex)
            {
                throw new Application.Exceptions.BaseServiceException("Error getting student growth trend", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<YearCountDto>> GetYearWiseAdmissionTrendAsync(Guid branchId)
        {
            try
            {
                _logger.LogInformation("GetYearWiseAdmissionTrend: BranchId {BranchId}", branchId);
                var cacheKey = $"students:trend:admission:{branchId}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning year-wise admission trend from cache");
                    return _cacheService.Get<IEnumerable<YearCountDto>>(cacheKey);
                }
                var query = _studentRepository.Query(s => s.BranchId == branchId && s.AdmissionDate.HasValue);
                var grouped = await query.GroupBy(s => s.AdmissionDate!.Value.Year).Select(g => new YearCountDto { Year = g.Key, Count = g.Count() }).OrderBy(x => x.Year).ToListAsync();
                _cacheService.Set(cacheKey, grouped);
                return grouped;
            }
            catch (Exception ex)
            {
                throw new Application.Exceptions.BaseServiceException("Error getting year-wise admission trend", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<RateAnalysisDto> DropoutRateAnalysisAsync(Guid branchId)
        {
            try
            {
                _logger.LogInformation("DropoutRateAnalysis: BranchId {BranchId}", branchId);
                var cacheKey = $"students:rate:dropout:{branchId}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning dropout rate analysis from cache");
                    return _cacheService.Get<RateAnalysisDto>(cacheKey);
                }
                var all = await _studentRepository.FindAllAsync(s => s.BranchId == branchId);
                var total = all.Count;
                var dropped = all.Count(s => s.StudentStatus != null && s.StudentStatus.Trim().Equals("DROPPED", StringComparison.OrdinalIgnoreCase));
                var rate = total > 0 ? (decimal)dropped / total * 100 : 0;
                var result = new RateAnalysisDto { Rate = Math.Round(rate, 2), TotalCount = total, Count = dropped, Label = "Dropout" };
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                throw new Application.Exceptions.BaseServiceException("Error in dropout rate analysis", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<RateAnalysisDto> PromotionSuccessRateAsync(Guid academicYearId)
        {
            try
            {
                _logger.LogInformation("PromotionSuccessRate: AcademicYearId {AcademicYearId}", academicYearId);
                var cacheKey = $"students:rate:promotion:{academicYearId}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning promotion success rate from cache");
                    return _cacheService.Get<RateAnalysisDto>(cacheKey);
                }
                var all = await _studentRepository.FindAllAsync(s => s.AcademicYearId == academicYearId);
                var total = all.Count;
                var promoted = all.Count(s => s.PromotedToClassId.HasValue);
                var rate = total > 0 ? (decimal)promoted / total * 100 : 0;
                var result = new RateAnalysisDto { Rate = Math.Round(rate, 2), TotalCount = total, Count = promoted, Label = "Promotion" };
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                throw new Application.Exceptions.BaseServiceException("Error in promotion success rate", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<RateAnalysisDto> StudentRetentionRateAsync()
        {
            try
            {
                _logger.LogInformation("StudentRetentionRate");
                const string cacheKey = "students:rate:retention";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning student retention rate from cache");
                    return _cacheService.Get<RateAnalysisDto>(cacheKey);
                }
                var all = await _studentRepository.GetAllAsync();
                var total = all.Count();
                var active = all.Count(s => s.IsActive == true);
                var rate = total > 0 ? (decimal)active / total * 100 : 0;
                var result = new RateAnalysisDto { Rate = Math.Round(rate, 2), TotalCount = total, Count = active, Label = "Retention" };
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                throw new Application.Exceptions.BaseServiceException("Error in student retention rate", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<RateAnalysisDto> TransferRateAnalysisAsync(Guid branchId)
        {
            try
            {
                _logger.LogInformation("TransferRateAnalysis: BranchId {BranchId}", branchId);
                var cacheKey = $"students:rate:transfer:{branchId}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning transfer rate analysis from cache");
                    return _cacheService.Get<RateAnalysisDto>(cacheKey);
                }
                var all = await _studentRepository.FindAllAsync(s => s.BranchId == branchId);
                var total = all.Count;
                var transferred = all.Count(s => s.StudentStatus != null && s.StudentStatus.Trim().Equals("TRANSFERRED", StringComparison.OrdinalIgnoreCase));
                var rate = total > 0 ? (decimal)transferred / total * 100 : 0;
                var result = new RateAnalysisDto { Rate = Math.Round(rate, 2), TotalCount = total, Count = transferred, Label = "Transfer" };
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                throw new Application.Exceptions.BaseServiceException("Error in transfer rate analysis", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<GenderRatioTrendPointDto>> GetGenderRatioTrendAsync(Guid branchId)
        {
            try
            {
                _logger.LogInformation("GetGenderRatioTrend: BranchId {BranchId}", branchId);
                var cacheKey = $"students:trend:gender:{branchId}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning gender ratio trend from cache");
                    return _cacheService.Get<IEnumerable<GenderRatioTrendPointDto>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s => s.BranchId == branchId && s.AdmissionDate.HasValue);
                var result = students
                    .GroupBy(s => s.AdmissionDate!.Value.Year)
                    .Select(g => new GenderRatioTrendPointDto
                    {
                        Year = g.Key,
                        MaleCount = g.Count(s => string.Equals(s.Gender?.Trim(), "MALE", StringComparison.OrdinalIgnoreCase)),
                        FemaleCount = g.Count(s => string.Equals(s.Gender?.Trim(), "FEMALE", StringComparison.OrdinalIgnoreCase)),
                        OtherCount = g.Count(s => s.Gender == null || string.IsNullOrWhiteSpace(s.Gender) || (!string.Equals(s.Gender.Trim(), "MALE", StringComparison.OrdinalIgnoreCase) && !string.Equals(s.Gender.Trim(), "FEMALE", StringComparison.OrdinalIgnoreCase)))
                    })
                    .OrderBy(x => x.Year).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                throw new Application.Exceptions.BaseServiceException("Error getting gender ratio trend", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<CasteTrendPointDto>> GetCasteTrendAnalysisAsync(Guid branchId)
        {
            try
            {
                _logger.LogInformation("GetCasteTrendAnalysis: BranchId {BranchId}", branchId);
                var cacheKey = $"students:trend:caste:{branchId}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning caste trend analysis from cache");
                    return _cacheService.Get<IEnumerable<CasteTrendPointDto>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s => s.BranchId == branchId && s.AdmissionDate.HasValue);
                var byYearCaste = students
                    .GroupBy(s => new { Year = s.AdmissionDate!.Value.Year, Caste = s.Caste?.Trim() ?? "Not specified" })
                    .Select(g => new CasteTrendPointDto { Year = g.Key.Year, Caste = g.Key.Caste, Count = g.Count() })
                    .OrderBy(x => x.Year).ThenBy(x => x.Caste).ToList();
                _cacheService.Set(cacheKey, byYearCaste);
                return byYearCaste;
            }
            catch (Exception ex)
            {
                throw new Application.Exceptions.BaseServiceException("Error getting caste trend analysis", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<DistributionItemDto>> UrbanRuralDistributionAnalysisAsync(Guid branchId)
        {
            try
            {
                _logger.LogInformation("UrbanRuralDistributionAnalysis: BranchId {BranchId}", branchId);
                var cacheKey = $"students:dist:urbanrural:{branchId}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning urban/rural distribution from cache");
                    return _cacheService.Get<IEnumerable<DistributionItemDto>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s => s.BranchId == branchId);
                var urban = students.Count(s => !string.IsNullOrWhiteSpace(s.City));
                var rural = students.Count - urban;
                var result = new List<DistributionItemDto> { new DistributionItemDto { Key = "Urban", Count = urban }, new DistributionItemDto { Key = "Rural", Count = rural } };
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                throw new Application.Exceptions.BaseServiceException("Error in urban/rural distribution analysis", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<RateAnalysisDto> AdmissionConversionRateAsync()
        {
            try
            {
                _logger.LogInformation("AdmissionConversionRate");
                const string cacheKey = "students:rate:conversion";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning admission conversion rate from cache");
                    return _cacheService.Get<RateAnalysisDto>(cacheKey);
                }
                var all = await _studentRepository.GetAllAsync();
                var withAdmission = all.Where(s => s.AdmissionDate.HasValue).ToList();
                var thisYear = DateTime.UtcNow.Year;
                var thisYearCount = withAdmission.Count(s => s.AdmissionDate!.Value.Year == thisYear);
                var lastYearCount = withAdmission.Count(s => s.AdmissionDate!.Value.Year == thisYear - 1);
                decimal rate = 0;
                if (lastYearCount > 0) rate = Math.Round((decimal)thisYearCount / lastYearCount * 100, 2);
                var result = new RateAnalysisDto
                {
                    Rate = rate,
                    TotalCount = lastYearCount + thisYearCount,
                    Count = thisYearCount,
                    Label = "Admission conversion (this year vs last year)",
                    Message = lastYearCount == 0 ? "No prior year admissions to compare." : null
                };
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                throw new Application.Exceptions.BaseServiceException("Error in admission conversion rate", ex, Application.Common.ExceptionCodes.Database);
            }
        }
    }
}
