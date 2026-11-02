using Application.Abstractions.Services.Exams;
using Application.Common.Helpers.MemoryCache;
using Application.ViewModels.ExamType;
using AutoMapper;
using Infrastructure.Services.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Exams
{
    /// <summary>Exam types (Unit test, Term 1, Final, ...). Rules: names are unique; a type in use cannot be deleted.</summary>
    public class ExamTypeService : CrudService<ExamType, ExamTypeRequest, ExamTypeResponse>, IExamTypeService
    {
        private readonly IExamRepository _exams;

        public ExamTypeService(ILogger<ExamTypeService> logger, IExamTypeRepository repository, IMapper mapper, IMemoryCacheHelper cache, IExamRepository exams)
            : base(repository, mapper, logger, cache)
        {
            _exams = exams;
        }

        protected override string EntityName => "ExamType";

        protected override TimeSpan? CacheDuration => TimeSpan.FromMinutes(30);

        protected override Task ValidateCreateAsync(ExamTypeRequest request) => EnsureNameIsFreeAsync(request.Name, exceptId: null);

        protected override Task ValidateUpdateAsync(ExamType existing, ExamTypeRequest request) => EnsureNameIsFreeAsync(request.Name, existing.Id);

        protected override async Task ValidateDeleteAsync(ExamType existing)
        {
            if (await _exams.IsExistAsync(e => e.ExamTypeId == existing.Id))
                throw Conflict($"Exam type '{existing.Name}' is used by exams.");
        }

        protected override void BeforeSave(ExamType entity) => entity.Name = entity.Name?.Trim();

        private async Task EnsureNameIsFreeAsync(string? requestedName, Guid? exceptId)
        {
            var name = requestedName?.Trim().ToLower();
            if (!string.IsNullOrEmpty(name)
                && await Repository.IsExistAsync(t => t.Name != null && t.Name.ToLower() == name && t.Id != exceptId))
                throw Conflict($"Exam type '{requestedName!.Trim()}' already exists.");
        }
    }
}
