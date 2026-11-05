using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities.Tenancy;

/// <summary>
/// Catalog table "organizations": one row per school group (trust). A school group IS the tenant: its school data
/// (branches, classes, students...) lives in its own PostgreSQL database named <see cref="DatabaseName"/>,
/// on the server from ConnectionStrings:PostgresConnection. A school group has many branches
/// (schools / campuses); every branch row carries this id as its GroupId.
/// </summary>
[Table("organizations")]
[Index(nameof(Identifier), IsUnique = true)]
[Index(nameof(DatabaseName), IsUnique = true)]
public class SchoolGroup : AuditableEntity
{
    /// <summary>URL-safe key, e.g. "bonzer-trust". Used in the X-Tenant header and in the database name.</summary>
    [Required]
    [MaxLength(40)]
    public string Identifier { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Code { get; set; }

    public string? Address { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(150)]
    public string? Email { get; set; }

    /// <summary>Only the database name is stored; server and credentials come from configuration.</summary>
    [Required]
    [MaxLength(63)]
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>Inactive school groups cannot sign in or call the API.</summary>
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
