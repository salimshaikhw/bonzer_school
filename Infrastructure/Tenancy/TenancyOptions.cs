namespace Infrastructure.Tenancy;

/// <summary>Configuration section "Tenancy".</summary>
public sealed class TenancyOptions
{
    public const string SectionName = "Tenancy";

    /// <summary>
    /// Name of the catalog (shared) database on the server from ConnectionStrings:PostgresConnection.
    /// Ignored when ConnectionStrings:CatalogConnection is set.
    /// </summary>
    public string CatalogDatabase { get; set; } = "bonzer_catalog";

    /// <summary>Tenant databases are named {prefix}{identifier}, with hyphens turned into underscores.</summary>
    public string TenantDatabasePrefix { get; set; } = "bonzer_school_";

    /// <summary>Header a Super Admin sends to act inside a tenant (value: the tenant identifier).</summary>
    public string TenantHeader { get; set; } = "X-Tenant";

    /// <summary>Apply pending tenant migrations to every active tenant database at startup.</summary>
    public bool MigrateTenantsOnStartup { get; set; } = true;

    /// <summary>How long a tenant lookup is cached. Deactivating a tenant through the API clears it at once.</summary>
    public TimeSpan TenantCacheDuration { get; set; } = TimeSpan.FromMinutes(5);
}
