using Application.Abstractions.Services.Exams;
using Application.Common.Helpers.MemoryCache;
using Application.ViewModels.GradeMaster;
using AutoMapper;
using Infrastructure.Services.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Exams
{
    /// <summary>
    /// Grade bands (A1: 91-100, A2: 81-90.99, ...). Rules: grade names are unique and bands do not overlap, so
    /// every percentage maps to at most one grade. Band shape (0-100, min &lt;= max) is checked by the validator.
    /// </summary>
    public class GradeMasterService : CrudService<GradeMaster, GradeMasterRequest, GradeMasterResponse>, IGradeMasterService
    {
        public GradeMasterService(ILogger<GradeMasterService> logger, IGradeMasterRepository repository, IMapper mapper, IMemoryCacheHelper cache)
            : base(repository, mapper, logger, cache)
        {
        }

        protected override string EntityName => "GradeMaster";

        protected override TimeSpan? CacheDuration => TimeSpan.FromMinutes(30);

        protected override Task ValidateCreateAsync(GradeMasterRequest request) => ValidateAsync(request, exceptId: null);

        protected override Task ValidateUpdateAsync(GradeMaster existing, GradeMasterRequest request) => ValidateAsync(request, existing.Id);

        protected override void BeforeSave(GradeMaster entity) => entity.Grade = entity.Grade?.Trim().ToUpperInvariant();

        private async Task ValidateAsync(GradeMasterRequest request, Guid? exceptId)
        {
            var grade = request.Grade?.Trim().ToUpperInvariant();
            if (!string.IsNullOrEmpty(grade) && await Repository.IsExistAsync(g => g.Grade == grade && g.Id != exceptId))
                throw Conflict($"Grade '{grade}' already exists.");

            if (request.MinPercent is { } min && request.MaxPercent is { } max)
            {
                var overlapping = await Repository.FindAsync(g => g.Id != exceptId && g.MinPercent <= max && min <= g.MaxPercent);
                if (overlapping != null)
                    throw Conflict($"{min}-{max}% overlaps grade '{overlapping.Grade}' ({overlapping.MinPercent}-{overlapping.MaxPercent}%).");
            }
        }
    }
}
