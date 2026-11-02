using Application.Abstractions.Services.Academics;
using Application.ViewModels.ClassTeacherMap;
using AutoMapper;
using Infrastructure.Services.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Academics
{
    /// <summary>
    /// Teachers assigned to a class / division. Without a subject the row means "class teacher"; with a subject,
    /// "teaches this subject to the class". Rules: class, staff and subject exist; the division belongs to the
    /// class; no duplicate rows; at most one class teacher per class + division + academic year.
    /// </summary>
    public class ClassTeacherMapService : CrudService<ClassTeacherMap, ClassTeacherMapRequest, ClassTeacherMapResponse>, IClassTeacherMapService
    {
        private readonly IAcademicStructure _structure;

        public ClassTeacherMapService(ILogger<ClassTeacherMapService> logger, IClassTeacherMapRepository repository, IMapper mapper,
            IAcademicStructure structure)
            : base(repository, mapper, logger)
        {
            _structure = structure;
        }

        protected override string EntityName => "ClassTeacherMap";

        protected override Task ValidateCreateAsync(ClassTeacherMapRequest request) => ValidateAsync(request, exceptId: null);

        protected override Task ValidateUpdateAsync(ClassTeacherMap existing, ClassTeacherMapRequest request) => ValidateAsync(request, existing.Id);

        private async Task ValidateAsync(ClassTeacherMapRequest request, Guid? exceptId)
        {
            var cls = await _structure.GetClassAsync(request.ClassId);
            await _structure.EnsureDivisionInClassAsync(request.DivisionId, cls.Id);
            await _structure.EnsureStaffExistsAsync(request.StaffId);
            await _structure.EnsureSubjectExistsAsync(request.SubjectId);

            if (await Repository.IsExistAsync(m => m.ClassId == request.ClassId && m.DivisionId == request.DivisionId
                                                   && m.StaffId == request.StaffId && m.SubjectId == request.SubjectId
                                                   && m.AcademicYear == request.AcademicYear && m.Id != exceptId))
                throw Conflict("This teacher is already assigned to the class in that role.");

            if (request.SubjectId == null
                && await Repository.IsExistAsync(m => m.ClassId == request.ClassId && m.DivisionId == request.DivisionId
                                                      && m.SubjectId == null && m.AcademicYear == request.AcademicYear
                                                      && m.Id != exceptId))
                throw Conflict("This class already has a class teacher for that academic year.");
        }
    }
}
