using Application.Abstractions.Services;
using Application.Common.Helpers.MemoryCache;
using Application.ViewModels.AcademicYear;
using AutoMapper;
using Infrastructure.Services.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Organization
{
    /// <summary>
    /// Academic years of the school group (e.g. "2026-27"). Dates and name format are checked by
    /// AcademicYearRequestValidator; here: the name is unique within the tenant.
    /// </summary>
    public class AcademicYearService : CrudService<AcademicYear, AcademicYearRequest, AcademicYearResponse>, IAcademicYearService
    {
        public AcademicYearService(IAcademicYearRepository repository, IMapper mapper, IMemoryCacheHelper cache, ILogger<AcademicYearService> logger)
            : base(repository, mapper, logger, cache)
        {
        }

        protected override string EntityName => "AcademicYear";

        protected override TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);

        protected override Task ValidateCreateAsync(AcademicYearRequest request) =>
            EnsureNameIsFreeAsync(request.YearName, exceptId: null);

        protected override Task ValidateUpdateAsync(AcademicYear existing, AcademicYearRequest request) =>
            EnsureNameIsFreeAsync(request.YearName, existing.Id);

        protected override void BeforeSave(AcademicYear entity)
        {
            entity.YearName = entity.YearName?.Trim();
            entity.IsActive ??= true;
        }

        private async Task EnsureNameIsFreeAsync(string? yearName, Guid? exceptId)
        {
            var name = yearName?.Trim().ToLower();
            if (string.IsNullOrEmpty(name))
                return;
            if (await Repository.IsExistAsync(y => y.YearName != null && y.YearName.ToLower() == name && y.Id != exceptId))
                throw Conflict($"Academic year '{yearName!.Trim()}' already exists.");
        }
    }
}
