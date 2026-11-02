using Application.ViewModels.Tenancy;

namespace Application.Abstractions.Tenancy;

/// <summary>Platform-level management of tenants (school groups). Used by the Super Admin only.</summary>
public interface ITenantService
{
    Task<IEnumerable<TenantResponse>> GetAllAsync();

    Task<TenantResponse> GetByIdAsync(Guid id);

    /// <summary>
    /// Registers the tenant in the catalog, creates and migrates its database, seeds reference data and
    /// the school group row, and creates its first Group Admin login.
    /// </summary>
    Task<TenantResponse> CreateAsync(CreateTenantRequest request);

    Task<TenantResponse> SetActiveAsync(Guid id, bool isActive);

    /// <summary>Sets the Google Workspace domain / Microsoft Entra directory trusted for external sign-in.</summary>
    Task<TenantResponse> SetExternalLoginAsync(Guid id, SetExternalLoginRequest request);

    /// <summary>Applies pending migrations to every active tenant database. Returns the identifiers migrated.</summary>
    Task<IReadOnlyList<string>> MigrateAllAsync(CancellationToken cancellationToken = default);
}
