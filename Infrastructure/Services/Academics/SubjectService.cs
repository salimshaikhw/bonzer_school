using Application.Abstractions.Services.Academics;
using Application.Common.Helpers.MemoryCache;
using Application.ViewModels.Subject;
using AutoMapper;
using Infrastructure.Services.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Academics
{
    /// <summary>Subjects of the school group. Rules: names are unique, and so are codes when given.</summary>
    public class SubjectService : CrudService<Subject, SubjectRequest, SubjectResponse>, ISubjectService
    {
        public SubjectService(ISubjectRepository repository, ILogger<SubjectService> logger, IMapper mapper, IMemoryCacheHelper cache)
            : base(repository, mapper, logger, cache)
        {
        }

        protected override string EntityName => "Subject";

        protected override TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);

        protected override Task ValidateCreateAsync(SubjectRequest request) => ValidateAsync(request, exceptId: null);

        protected override Task ValidateUpdateAsync(Subject existing, SubjectRequest request) => ValidateAsync(request, existing.Id);

        protected override void BeforeSave(Subject entity)
        {
            entity.Name = entity.Name?.Trim();
            entity.Code = string.IsNullOrWhiteSpace(entity.Code) ? null : entity.Code.Trim().ToUpperInvariant();
        }

        private async Task ValidateAsync(SubjectRequest request, Guid? exceptId)
        {
            var name = request.Name?.Trim().ToLower();
            if (!string.IsNullOrEmpty(name)
                && await Repository.IsExistAsync(s => s.Name != null && s.Name.ToLower() == name && s.Id != exceptId))
                throw Conflict($"Subject '{request.Name!.Trim()}' already exists.");

            var code = request.Code?.Trim().ToUpperInvariant();
            if (!string.IsNullOrEmpty(code)
                && await Repository.IsExistAsync(s => s.Code == code && s.Id != exceptId))
                throw Conflict($"Subject code '{code}' is already used.");
        }
    }
}
