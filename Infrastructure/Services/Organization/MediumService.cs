using Application.Abstractions.Services;
using Application.ViewModels.Medium;
using AutoMapper;
using Infrastructure.Services.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Organization
{
    /// <summary>
    /// Mediums of instruction (English, Marathi, ...). Shared by all tenants (catalog database), so they are
    /// not cached per tenant. Rule: names are unique.
    /// </summary>
    public class MediumService : CrudService<Medium, MediumRequest, MediumResponse>, IMediumService
    {
        public MediumService(IMediumRepository repository, ILogger<MediumService> logger, IMapper mapper)
            : base(repository, mapper, logger)
        {
        }

        protected override string EntityName => "Medium";

        protected override Task ValidateCreateAsync(MediumRequest request) => EnsureNameIsFreeAsync(request.Name, exceptId: null);

        protected override Task ValidateUpdateAsync(Medium existing, MediumRequest request) => EnsureNameIsFreeAsync(request.Name, existing.Id);

        protected override void BeforeSave(Medium entity) => entity.Name = entity.Name?.Trim();

        private async Task EnsureNameIsFreeAsync(string? requestedName, Guid? exceptId)
        {
            var name = requestedName?.Trim().ToLower();
            if (!string.IsNullOrEmpty(name)
                && await Repository.IsExistAsync(m => m.Name != null && m.Name.ToLower() == name && m.Id != exceptId))
                throw Conflict($"Medium '{requestedName!.Trim()}' already exists.");
        }
    }
}
