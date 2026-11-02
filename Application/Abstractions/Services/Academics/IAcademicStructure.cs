using System;
using System.Threading.Tasks;
using Domain.Entities.Academics;

namespace Application.Abstractions.Services.Academics
{
    /// <summary>
    /// Checks that the academic records a request refers to exist and fit together (branch -> class ->
    /// division, subjects, staff). Shared by the Academics services so the rules are written once.
    /// Every method throws a validation error (400) naming the offending reference.
    /// </summary>
    public interface IAcademicStructure
    {
        Task EnsureBranchExistsAsync(Guid? branchId);

        /// <summary>Returns the class; fails when the id is missing or unknown.</summary>
        Task<Class> GetClassAsync(Guid? classId);

        /// <summary>No-op when <paramref name="divisionId"/> is null; otherwise the division must belong to the class.</summary>
        Task EnsureDivisionInClassAsync(Guid? divisionId, Guid classId);

        /// <summary>No-op when <paramref name="subjectId"/> is null.</summary>
        Task EnsureSubjectExistsAsync(Guid? subjectId);

        /// <summary>No-op when <paramref name="staffId"/> is null.</summary>
        Task EnsureStaffExistsAsync(Guid? staffId);
    }
}
