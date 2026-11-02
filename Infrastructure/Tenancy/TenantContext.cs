using Application.Abstractions.Tenancy;

namespace Infrastructure.Tenancy;

/// <summary>Scoped holder of the current tenant. Set once per request (middleware) or per job scope.</summary>
public sealed class TenantContext : ITenantContext
{
    public TenantInfo? Current { get; private set; }

    /// <summary>
    /// Selects the tenant for this scope. Must happen before the scope resolves BonzerSchoolDbContext,
    /// and cannot be changed afterwards, so a scope never mixes two tenants' data.
    /// </summary>
    public void SetTenant(TenantInfo tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        if (Current != null && Current.Id != tenant.Id)
            throw new InvalidOperationException($"The tenant of this scope is already '{Current.Identifier}'.");
        Current = tenant;
    }
}
