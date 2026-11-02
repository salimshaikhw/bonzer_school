using Application.Abstractions.Services.Academics;
using Application.ViewModels.ClassSubjectMap;
using AutoMapper;
using Infrastructure.Services.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Academics
{
    /// <summary>
    /// Which subjects a class (optionally one division) studies in an academic year. Rules: class and subject
    /// exist, the division belongs to the class, and the same subject is not assigned twice.
    /// </summary>
    public class ClassSubjectMapService : CrudService<ClassSubjectMap, ClassSubjectMapRequest, ClassSubjectMapResponse>, IClassSubjectMapService
    {
        private readonly IAcademicStructure _structure;

        public ClassSubjectMapService(ILogger<ClassSubjectMapService> logger, IClassSubjectMapRepository repository, IMapper mapper,
            IAcademicStructure structure)
            : base(repository, mapper, logger)
        {
            _structure = structure;
        }

        protected override string EntityName => "ClassSubjectMap";

        protected override Task ValidateCreateAsync(ClassSubjectMapRequest request) => ValidateAsync(request, exceptId: null);

        protected override Task ValidateUpdateAsync(ClassSubjectMap existing, ClassSubjectMapRequest request) => ValidateAsync(request, existing.Id);

        private async Task ValidateAsync(ClassSubjectMapRequest request, Guid? exceptId)
        {
            var cls = await _structure.GetClassAsync(request.ClassId);
            await _structure.EnsureDivisionInClassAsync(request.DivisionId, cls.Id);
            await _structure.EnsureSubjectExistsAsync(request.SubjectId);

            if (await Repository.IsExistAsync(m => m.ClassId == request.ClassId && m.DivisionId == request.DivisionId
                                                   && m.SubjectId == request.SubjectId && m.AcademicYear == request.AcademicYear
                                                   && m.Id != exceptId))
                throw Conflict("This subject is already assigned to the class for that academic year.");
        }
    }
}
