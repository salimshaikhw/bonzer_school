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
    /// <summary>
    /// Everything the student services share (Parameter Object): repositories, mapper, cache, clock and the
    /// academic-structure checks. Registered once in DI so each focused service takes one dependency.
    /// </summary>
    public sealed record StudentServiceDependencies(
        IStudentRepository Students,
        IUnitOfWork UnitOfWork,
        IMapper Mapper,
        ICacheService Cache,
        ITeacherSubjectMapRepository TeacherSubjectMaps,
        IInvoiceRepository Invoices,
        IAttendanceRepository Attendances,
        IClassRepository Classes,
        IDivisionRepository Divisions,
        IBranchRepository Branches,
        IAcademicYearRepository AcademicYears,
        IAcademicStructure Structure,
        TimeProvider Clock);

    /// <summary>
    /// Base of the student services. The student module is split by responsibility (SRP / ISP):
    ///   StudentService            - admission, edits and the enrolment lifecycle (IStudentService)
    ///   StudentQueryService       - lookups, filters and search (IStudentQueryService)
    ///   StudentAnalyticsService   - distributions, trends and rates (IStudentAnalyticsService)
    ///   StudentReportService      - statutory reports and per-student summaries (IStudentReportService)
    ///   StudentBulkUploadService  - Excel template and import (IStudentBulkUploadService)
    /// Shared helpers (placement / identifier rules, cache keys, mapping) live here so every path applies them.
    /// </summary>
    public abstract class StudentServiceBase
    {
        protected readonly ILogger _logger;
        protected readonly IMapper _mapper;
        protected readonly IStudentRepository _studentRepository;
        protected readonly IUnitOfWork _unitOfWork;
        protected readonly ICacheService _cacheService;
        protected readonly ITeacherSubjectMapRepository _teacherSubjectMapRepository;
        protected readonly IInvoiceRepository _invoiceRepository;
        protected readonly IAttendanceRepository _attendanceRepository;
        protected readonly IClassRepository _classRepository;
        protected readonly IDivisionRepository _divisionRepository;
        protected readonly IBranchRepository _branchRepository;
        protected readonly IAcademicYearRepository _academicYearRepository;
        protected readonly IAcademicStructure _structure;
        protected readonly TimeProvider _clock;

        protected StudentServiceBase(StudentServiceDependencies deps, ILogger logger)
        {
            _logger = logger;
            _mapper = deps.Mapper;
            _studentRepository = deps.Students;
            _unitOfWork = deps.UnitOfWork;
            _cacheService = deps.Cache;
            _teacherSubjectMapRepository = deps.TeacherSubjectMaps;
            _invoiceRepository = deps.Invoices;
            _attendanceRepository = deps.Attendances;
            _classRepository = deps.Classes;
            _divisionRepository = deps.Divisions;
            _branchRepository = deps.Branches;
            _academicYearRepository = deps.AcademicYears;
            _structure = deps.Structure;
            _clock = deps.Clock;
        }

        /// <summary>Today in UTC, from the injected clock (testable).</summary>
        protected DateOnly Today => DateOnly.FromDateTime(_clock.GetUtcNow().UtcDateTime);


        protected async Task EnsureConsistentPlacementAsync(Guid? branchId, Guid? classId, Guid? divisionId)
        {
            await _structure.EnsureBranchExistsAsync(branchId);
            if (classId != null)
            {
                var cls = await _structure.GetClassAsync(classId);
                if (cls.BranchId != branchId)
                    throw new BaseServiceException($"Class '{cls.Name}' is not part of the student's branch.", ExceptionCodes.Validation);
                await _structure.EnsureDivisionInClassAsync(divisionId, cls.Id);
            }
            else if (divisionId != null)
            {
                throw new BaseServiceException("Select the class of the division.", ExceptionCodes.Validation);
            }
        }

        protected static void EnsureStatusUnchanged(Student student, string? requestedStatus)
        {
            if (!string.IsNullOrWhiteSpace(requestedStatus) && requestedStatus.Trim().ToUpperInvariant() != StudentStatuses.Of(student))
                throw new BaseServiceException(
                    "Change a student's status with the lifecycle actions (activate, suspend, drop, promote, pass out, TC, re-enrol), not by editing the student.",
                    ExceptionCodes.Operation);
        }

        /// <summary>Stores identifiers in one canonical form so uniqueness checks and searches match.</summary>
        protected static void NormalizeIdentifiers(Student student)
        {
            student.ChildAadhaar = Aadhaar.Normalize(student.ChildAadhaar);
            student.AdmissionNo = string.IsNullOrWhiteSpace(student.AdmissionNo) ? null : student.AdmissionNo.Trim();
            student.RollNumber = string.IsNullOrWhiteSpace(student.RollNumber) ? null : student.RollNumber.Trim();
        }

        // ---- Cache ----

        protected void InvalidateStudentCaches(Guid id, Student student) => RemoveCacheKeys(StudentCacheKeys(student));

        /// <summary>Every cached list or item the student appears in, for its current placement and status.</summary>
        protected static IEnumerable<string> StudentCacheKeys(Student student)
        {
            yield return "students:all";
            yield return "students:count:total";
            yield return $"student:{student.Id}";
            if (student.DivisionId.HasValue) yield return $"students:division:{student.DivisionId.Value}";
            if (student.ClassId.HasValue) yield return $"students:class:{student.ClassId.Value}";
            if (student.BranchId.HasValue) yield return $"students:branch:{student.BranchId.Value}";
            if (student.AcademicYearId.HasValue) yield return $"students:academicyear:{student.AcademicYearId.Value}";
            yield return "students:active";
            yield return "students:inactive";
            yield return "students:dropped";
            yield return "students:transferred";
            yield return "students:passedout";
            if (!string.IsNullOrWhiteSpace(student.StudentStatus))
                yield return $"students:status:{student.StudentStatus.Trim()}";
        }

        protected void RemoveCacheKeys(IEnumerable<string> keys)
        {
            foreach (var key in keys.Distinct())
                _cacheService.Remove(key);
        }

        protected static (int? Years, int? Months) GetAgeFromDob(DateOnly dob)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var totalMonths = (today.Year - dob.Year) * 12 + (today.Month - dob.Month);
            if (today.Day < dob.Day) totalMonths--;
            if (totalMonths < 0) return (null, null);
            var years = totalMonths / 12;
            var months = totalMonths % 12;
            return (years, months);
        }

        protected static (int? Years, int? Months) GetDurationBetween(DateOnly from, DateOnly to)
        {
            var totalMonths = (to.Year - from.Year) * 12 + (to.Month - from.Month);
            if (to.Day < from.Day) totalMonths--;
            if (totalMonths < 0) return (null, null);
            var years = totalMonths / 12;
            var months = totalMonths % 12;
            return (years, months);
        }

        protected static string? BuildFullName(string? first, string? middle, string? last)
        {
            var parts = new[] { first?.Trim(), middle?.Trim(), last?.Trim() }.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            return parts.Count == 0 ? null : string.Join(" ", parts);
        }

        protected StudentResponse MapStudentToResponse(Student s) => _mapper.Map<StudentResponse>(s);
    }
}
