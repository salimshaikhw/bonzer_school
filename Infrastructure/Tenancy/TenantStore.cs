using Application.Abstractions.Tenancy;
using Infrastructure.Persistence.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Infrastructure.Tenancy;

/// <summary>Reads tenants from the catalog, cached because every authenticated request needs one.</summary>
public interface ITenantStore
{
    Task<TenantInfo?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TenantInfo?> FindByIdentifierAsync(string identifier, CancellationToken cancellationToken = default);

    /// <summary>Drops cached copies after the tenant row changed.</summary>
    void Invalidate(Guid id, string identifier);
}

public sealed class TenantStore(CatalogDbContext catalog, IMemoryCache cache, IOptions<TenancyOptions> options) : ITenantStore
{
    public Task<TenantInfo?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetOrLoadAsync(IdKey(id), t => t.Id == id, cancellationToken);

    public Task<TenantInfo?> FindByIdentifierAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var normalized = identifier.Trim().ToLowerInvariant();
        return GetOrLoadAsync(IdentifierKey(normalized), t => t.Identifier == normalized, cancellationToken);
    }

    public void Invalidate(Guid id, string identifier)
    {
        cache.Remove(IdKey(id));
        cache.Remove(IdentifierKey(identifier));
    }

    private async Task<TenantInfo?> GetOrLoadAsync(
        string key, System.Linq.Expressions.Expression<Func<SchoolGroup, bool>> match, CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(key, out TenantInfo? cached))
            return cached;

        var tenant = await catalog.SchoolGroups.AsNoTracking()
            .Where(match)
            .Select(t => new TenantInfo(t.Id, t.Identifier, t.Name, t.DatabaseName, t.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        // Misses are not cached, so a tenant created a moment ago is found straight away.
        if (tenant != null)
            cache.Set(key, tenant, options.Value.TenantCacheDuration);
        return tenant;
    }

    private static string IdKey(Guid id) => $"tenancy:id:{id:N}";

    private static string IdentifierKey(string identifier) => $"tenancy:identifier:{identifier}";
}
