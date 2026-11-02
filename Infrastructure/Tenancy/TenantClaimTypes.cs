namespace Infrastructure.Tenancy;

/// <summary>JWT claims that tie a login to its tenant. Issued only for tenant users (not the Super Admin).</summary>
public static class TenantClaimTypes
{
    /// <summary>Catalog tenants.id (Guid). The API trusts only this claim to pick the tenant database.</summary>
    public const string TenantId = "tenant_id";

    /// <summary>Tenant identifier, informational for the client.</summary>
    public const string Tenant = "tenant";
}
