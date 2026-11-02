using Application.Abstractions.Services.Academics;
using Application.ViewModels.TeacherSubjectMap;
using AutoMapper;
using Infrastructure.Services.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Academics
{
    /// <summary>
    /// Subjects a teacher is qualified for / teaches, optionally for one class and division. Rules: staff and
    /// subject exist; the class / division fit together; no duplicates (also enforced by a unique index).
    /// </summary>
    public class TeacherSubjectMapService : CrudService<TeacherSubjectMap, TeacherSubjectMapRequest, TeacherSubjectMapResponse>, ITeacherSubjectMapService
    {
        private readonly IAcademicStructure _structure;

        public TeacherSubjectMapService(ILogger<TeacherSubjectMapService> logger, ITeacherSubjectMapRepository repository, IMapper mapper,
            IAcademicStructure structure)
            : base(repository, mapper, logger)
        {
            _structure = structure;
        }

        protected override string EntityName => "TeacherSubjectMap";

        protected override Task ValidateCreateAsync(TeacherSubjectMapRequest request) => ValidateAsync(request, exceptId: null);

        protected override Task ValidateUpdateAsync(TeacherSubjectMap existing, TeacherSubjectMapRequest request) => ValidateAsync(request, existing.Id);

        private async Task ValidateAsync(TeacherSubjectMapRequest request, Guid? exceptId)
        {
            await _structure.EnsureStaffExistsAsync(request.StaffId);
            await _structure.EnsureSubjectExistsAsync(request.SubjectId);
            if (request.ClassId != null)
            {
                var cls = await _structure.GetClassAsync(request.ClassId);
                await _structure.EnsureDivisionInClassAsync(request.DivisionId, cls.Id);
            }
            else if (request.DivisionId != null)
            {
                throw RuleViolation("Select the class of the division.");
            }

            if (await Repository.IsExistAsync(m => m.StaffId == request.StaffId && m.SubjectId == request.SubjectId
                                                   && m.ClassId == request.ClassId && m.DivisionId == request.DivisionId
                                                   && m.AcademicYear == request.AcademicYear && m.Id != exceptId))
                throw Conflict("This teacher is already mapped to that subject.");
        }
    }
}
