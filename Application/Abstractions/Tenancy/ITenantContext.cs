namespace Application.Abstractions.Tenancy;

/// <summary>
/// The school group (tenant) the current request or background job works on. Filled in once per
/// scope: by the API's tenant middleware from the JWT ("tenant_id" claim), or by code that runs a
/// job for a tenant. Every tenant has its own database; BonzerSchoolDbContext connects to it.
/// </summary>
public interface ITenantContext
{
    /// <summary>The tenant, or null when the request is not tied to one (login, platform admin).</summary>
    TenantInfo? Current { get; }

    bool IsResolved => Current != null;

    Guid? TenantId => Current?.Id;

    /// <summary>Prefix for shared caches so one school never reads another school's cache entries.</summary>
    string CacheKeyPrefix => Current == null ? "platform:" : $"tenant:{Current.Id:N}:";
}

/// <summary>Catalog data needed to serve a tenant (a cached snapshot of the "tenants" row).</summary>
public sealed record TenantInfo(Guid Id, string Identifier, string Name, string DatabaseName, bool IsActive);
