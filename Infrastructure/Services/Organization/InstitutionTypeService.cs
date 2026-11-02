using Application.Abstractions.Services;
using Application.ViewModels.InstitutionType;
using AutoMapper;
using Infrastructure.Services.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Organization
{
    /// <summary>
    /// Institution types (Primary school, Junior college, University, Coaching, ...). Shared by all tenants
    /// (catalog database), so they are not cached per tenant. Rule: names are unique.
    /// </summary>
    public class InstitutionTypeService : CrudService<InstitutionType, InstitutionTypeRequest, InstitutionTypeResponse>, IInstitutionTypeService
    {
        public InstitutionTypeService(IInstitutionTypeRepository repository, ILogger<InstitutionTypeService> logger, IMapper mapper)
            : base(repository, mapper, logger)
        {
        }

        protected override string EntityName => "InstitutionType";

        protected override Task ValidateCreateAsync(InstitutionTypeRequest request) => EnsureNameIsFreeAsync(request.Name, exceptId: null);

        protected override Task ValidateUpdateAsync(InstitutionType existing, InstitutionTypeRequest request) => EnsureNameIsFreeAsync(request.Name, existing.Id);

        protected override void BeforeSave(InstitutionType entity) => entity.Name = entity.Name?.Trim();

        private async Task EnsureNameIsFreeAsync(string? requestedName, Guid? exceptId)
        {
            var name = requestedName?.Trim().ToLower();
            if (!string.IsNullOrEmpty(name)
                && await Repository.IsExistAsync(t => t.Name != null && t.Name.ToLower() == name && t.Id != exceptId))
                throw Conflict($"Institution type '{requestedName!.Trim()}' already exists.");
        }
    }
}
