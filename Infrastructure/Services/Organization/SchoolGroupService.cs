using Application.Abstractions.Services;
using Application.Abstractions.Tenancy;
using Application.Common.Helpers.MemoryCache;
using Application.ViewModels.SchoolGroup;
using AutoMapper;
using Infrastructure.Services.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Organization
{
    /// <summary>
    /// The school group (trust) of the current tenant. Each tenant database holds exactly one school group
    /// row, created together with the tenant and sharing its id. Business rules:
    ///   - it cannot be created or deleted here (create / deactivate the tenant instead: /api/tenants);
    ///   - only the current tenant's own group can be read or changed.
    /// </summary>
    public class SchoolGroupService : CrudService<SchoolGroup, UpdateSchoolGroupInput, SchoolGroupDto>, ISchoolGroupService
    {
        private readonly ITenantContext _tenantContext;

        public SchoolGroupService(ISchoolGroupRepository repository, IMapper mapper, IMemoryCacheHelper cache, ILogger<SchoolGroupService> logger, ITenantContext tenantContext)
            : base(repository, mapper, logger, cache)
        {
            _tenantContext = tenantContext;
        }

        protected override string EntityName => "SchoolGroup";

        protected override TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);

        public Task<SchoolGroupDto> CreateAsync(CreateSchoolGroupInput input) =>
            throw Conflict("A school group is created together with its tenant (POST /api/tenants); a tenant has exactly one school group.");

        public override async Task<SchoolGroupDto> GetByIdAsync(Guid id)
        {
            EnsureOwnGroup(id);
            return await base.GetByIdAsync(id);
        }

        protected override Task ValidateUpdateAsync(SchoolGroup existing, UpdateSchoolGroupInput request)
        {
            EnsureOwnGroup(existing.Id);
            return Task.CompletedTask;
        }

        protected override Task ValidateDeleteAsync(SchoolGroup existing) =>
            throw Conflict("The school group cannot be deleted. Deactivate the tenant instead (PUT /api/tenants/{id}/status).");

        /// <summary>Another group id inside this tenant cannot exist; report it as not found rather than leak anything.</summary>
        private void EnsureOwnGroup(Guid id)
        {
            if (_tenantContext.TenantId is { } tenantId && id != tenantId)
                throw new Application.Exceptions.BaseServiceException($"School group {id} not found.", Application.Common.ExceptionCodes.ItemNotFound);
        }
    }
}
