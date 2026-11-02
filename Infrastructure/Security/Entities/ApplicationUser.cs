using Application.Common;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Security.Entities;

/// <summary>
/// Login, stored in the catalog database. A login belongs to exactly one tenant (school group), whose id
/// is issued as the "tenant_id" JWT claim; platform users (Super Admin) have no tenant.
/// Email addresses are unique across all tenants.
/// </summary>
public class ApplicationUser : IdentityUser, IAuditable
{
    /// <summary>Catalog tenants.id; null for platform users.</summary>
    public Guid? TenantId { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    public List<RefreshToken> RefreshTokens { get; set; } = new();

    public string? DisplayName { get; set; }
    public DateTime CreatedOn { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedOn { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}