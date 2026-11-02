using Application.Abstractions.Services;
using Application.Abstractions.Tenancy;
using Application.Common.Helpers.MemoryCache;
using Application.ViewModels.Branch;
using AutoMapper;
using Infrastructure.Services.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Organization
{
    /// <summary>
    /// Branches (campuses / schools) of the tenant's school group. Business rules:
    ///   - a branch always belongs to the tenant's own school group (the group row has the tenant's id);
    ///   - branch names are unique within the group, and so is the SchoolUid (e.g. UDISE code) when given.
    /// </summary>
    public class BranchService : CrudService<Branch, BranchRequest, BranchResponse>, IBranchService
    {
        private readonly ITenantContext _tenantContext;

        public BranchService(IBranchRepository repository, IMapper mapper, IMemoryCacheHelper cache, ILogger<BranchService> logger, ITenantContext tenantContext)
            : base(repository, mapper, logger, cache)
        {
            _tenantContext = tenantContext;
        }

        protected override string EntityName => "Branch";

        protected override TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);

        protected override Task ValidateCreateAsync(BranchRequest request) => ValidateAsync(request, exceptId: null);

        protected override Task ValidateUpdateAsync(Branch existing, BranchRequest request) => ValidateAsync(request, existing.Id);

        protected override void BeforeSave(Branch entity)
        {
            entity.Name = entity.Name?.Trim();
            entity.SchoolUid = string.IsNullOrWhiteSpace(entity.SchoolUid) ? null : entity.SchoolUid.Trim();
            // The tenant database holds exactly one school group, whose id is the tenant id.
            if (_tenantContext.TenantId is { } groupId)
                entity.GroupId = groupId;
        }

        private async Task ValidateAsync(BranchRequest request, Guid? exceptId)
        {
            if (request.GroupId is { } groupId && _tenantContext.TenantId is { } tenantId && groupId != tenantId)
                throw RuleViolation("A branch can only belong to this school group.");

            var name = request.Name?.Trim().ToLower();
            if (!string.IsNullOrEmpty(name)
                && await Repository.IsExistAsync(b => b.Name != null && b.Name.ToLower() == name && b.Id != exceptId))
                throw Conflict($"A branch named '{request.Name!.Trim()}' already exists.");

            var schoolUid = request.Schooluid?.Trim();
            if (!string.IsNullOrEmpty(schoolUid)
                && await Repository.IsExistAsync(b => b.SchoolUid == schoolUid && b.Id != exceptId))
                throw Conflict($"Another branch already uses school code '{schoolUid}'.");
        }
    }
}
