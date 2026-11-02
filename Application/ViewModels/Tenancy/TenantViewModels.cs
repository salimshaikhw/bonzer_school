namespace Application.ViewModels.Tenancy;

public class CreateTenantRequest
{
    /// <summary>URL-safe key, e.g. "bonzer-trust": lower-case letters, digits and hyphens. Sent as the X-Tenant header.</summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>School group (trust) name.</summary>
    public string Name { get; set; } = string.Empty;

    public string? Code { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }

    /// <summary>First Group Admin login of the new tenant.</summary>
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
    public string AdminFirstName { get; set; } = "Group";
    public string AdminLastName { get; set; } = "Admin";
}

public class TenantResponse
{
    public Guid Id { get; set; }
    public string Identifier { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? GoogleWorkspaceDomain { get; set; }
    public Guid? MicrosoftEntraTenantId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SetTenantStatusRequest
{
    public bool IsActive { get; set; }
}

/// <summary>
/// Directories whose accounts may sign in with Google / Microsoft and be matched to an existing login by
/// email. Null turns matching off; users can still link their own account after signing in.
/// </summary>
public class SetExternalLoginRequest
{
    public string? GoogleWorkspaceDomain { get; set; }
    public Guid? MicrosoftEntraTenantId { get; set; }
}
