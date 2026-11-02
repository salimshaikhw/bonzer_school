using Application.Abstractions.Services.Academics;
using Application.Common;
using Application.Exceptions;

namespace Infrastructure.Services.Academics
{
    /// <inheritdoc cref="IAcademicStructure"/>
    public class AcademicStructure : IAcademicStructure
    {
        private readonly IBranchRepository _branches;
        private readonly IClassRepository _classes;
        private readonly IDivisionRepository _divisions;
        private readonly ISubjectRepository _subjects;
        private readonly IStaffRepository _staff;

        public AcademicStructure(IBranchRepository branches, IClassRepository classes, IDivisionRepository divisions,
            ISubjectRepository subjects, IStaffRepository staff)
        {
            _branches = branches;
            _classes = classes;
            _divisions = divisions;
            _subjects = subjects;
            _staff = staff;
        }

        public async Task EnsureBranchExistsAsync(Guid? branchId)
        {
            if (branchId is not { } id || !await _branches.IsExistAsync(b => b.Id == id))
                throw Invalid($"Branch {branchId} does not exist.");
        }

        public async Task<Class> GetClassAsync(Guid? classId)
        {
            if (classId is not { } id)
                throw Invalid("A class is required.");
            return await _classes.FindAsync(c => c.Id == id)
                   ?? throw Invalid($"Class {id} does not exist.");
        }

        public async Task EnsureDivisionInClassAsync(Guid? divisionId, Guid classId)
        {
            if (divisionId is not { } id)
                return;
            var division = await _divisions.FindAsync(d => d.Id == id)
                           ?? throw Invalid($"Division {id} does not exist.");
            if (division.ClassId != classId)
                throw Invalid($"Division '{division.Name}' does not belong to the selected class.");
        }

        public async Task EnsureSubjectExistsAsync(Guid? subjectId)
        {
            if (subjectId is { } id && !await _subjects.IsExistAsync(s => s.Id == id))
                throw Invalid($"Subject {id} does not exist.");
        }

        public async Task EnsureStaffExistsAsync(Guid? staffId)
        {
            if (staffId is { } id && !await _staff.IsExistAsync(s => s.Id == id))
                throw Invalid($"Staff member {id} does not exist.");
        }

        private static BaseServiceException Invalid(string message) => new(message, ExceptionCodes.Validation);
    }
}
