using Application.Abstractions.Services.Exams;
using Application.Common.Helpers.MemoryCache;
using Application.ViewModels.ExamComponentType;
using AutoMapper;
using Infrastructure.Services.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Exams
{
    /// <summary>Component types (Theory, Practical, Oral, ...). Rules: names and codes are unique; a type in use cannot be deleted.</summary>
    public class ExamComponentTypeService : CrudService<ExamComponentType, ExamComponentTypeRequest, ExamComponentTypeResponse>, IExamComponentTypeService
    {
        private readonly IExamSubjectComponentRepository _components;

        public ExamComponentTypeService(ILogger<ExamComponentTypeService> logger, IExamComponentTypeRepository repository, IMapper mapper,
            IMemoryCacheHelper cache, IExamSubjectComponentRepository components)
            : base(repository, mapper, logger, cache)
        {
            _components = components;
        }

        protected override string EntityName => "ExamComponentType";

        protected override TimeSpan? CacheDuration => TimeSpan.FromMinutes(30);

        protected override Task ValidateCreateAsync(ExamComponentTypeRequest request) => ValidateAsync(request, exceptId: null);

        protected override Task ValidateUpdateAsync(ExamComponentType existing, ExamComponentTypeRequest request) => ValidateAsync(request, existing.Id);

        protected override async Task ValidateDeleteAsync(ExamComponentType existing)
        {
            if (await _components.IsExistAsync(c => c.ComponentTypeId == existing.Id))
                throw Conflict($"Component type '{existing.Name}' is used by exam subjects.");
        }

        protected override void BeforeSave(ExamComponentType entity)
        {
            entity.Name = entity.Name.Trim();
            entity.Code = string.IsNullOrWhiteSpace(entity.Code) ? null : entity.Code.Trim().ToUpperInvariant();
        }

        private async Task ValidateAsync(ExamComponentTypeRequest request, Guid? exceptId)
        {
            var name = request.Name?.Trim().ToLower();
            if (!string.IsNullOrEmpty(name)
                && await Repository.IsExistAsync(t => t.Name.ToLower() == name && t.Id != exceptId))
                throw Conflict($"Component type '{request.Name!.Trim()}' already exists.");

            var code = request.Code?.Trim().ToUpperInvariant();
            if (!string.IsNullOrEmpty(code) && await Repository.IsExistAsync(t => t.Code == code && t.Id != exceptId))
                throw Conflict($"Component code '{code}' is already used.");
        }
    }
}
