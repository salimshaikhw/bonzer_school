using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities.Tenancy;

/// <summary>
/// Catalog table: one row per tenant (school group / trust). Each tenant's school data lives in its own
/// PostgreSQL database named <see cref="DatabaseName"/>, on the server from ConnectionStrings:PostgresConnection.
/// The tenant's school_groups row uses the same Id.
/// </summary>
[Table("tenants")]
[Index(nameof(Identifier), IsUnique = true)]
[Index(nameof(DatabaseName), IsUnique = true)]
public class Tenant : AuditableEntity
{
    /// <summary>URL-safe key, e.g. "bonzer-trust". Used in the X-Tenant header and in the database name.</summary>
    [Required]
    [MaxLength(40)]
    public string Identifier { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Only the database name is stored; server and credentials come from configuration.</summary>
    [Required]
    [MaxLength(63)]
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>Inactive tenants cannot sign in or call the API.</summary>
    [DefaultValue(true)]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Google Workspace domain (the verified "hd" claim, e.g. "school.edu") whose accounts may sign in with
    /// Google and be matched to an existing login by email. Null: only logins that linked their Google
    /// account themselves can use Google sign-in.
    /// </summary>
    [MaxLength(253)]
    public string? GoogleWorkspaceDomain { get; set; }

    /// <summary>
    /// Microsoft Entra directory id (the "tid" claim) whose accounts may sign in with Microsoft and be
    /// matched to an existing login by email. Null: only self-linked Microsoft accounts can sign in.
    /// </summary>
    public Guid? MicrosoftEntraTenantId { get; set; }
}
