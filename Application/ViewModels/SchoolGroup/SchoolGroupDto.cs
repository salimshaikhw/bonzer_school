using System;

namespace Application.ViewModels.SchoolGroup
{
    /// <summary>A school group (trust). It is the tenant: one school group, one database, many branches.</summary>
    public class SchoolGroupDto : AuditableResponse
    {
        /// <summary>URL-safe key, sent as the X-Tenant header by the Super Admin.</summary>
        public string Identifier { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Code { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string DatabaseName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string? GoogleWorkspaceDomain { get; set; }
        public Guid? MicrosoftEntraTenantId { get; set; }
    }

    /// <summary>Creates the school group, its database and its first Group Admin login (Super Admin only).</summary>
    public class CreateSchoolGroupInput
    {
        /// <summary>URL-safe key, e.g. "bonzer-trust": 3-40 lower-case letters, digits and hyphens.</summary>
        public string Identifier { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }

        public string AdminEmail { get; set; } = string.Empty;
        public string AdminPassword { get; set; } = string.Empty;
        public string AdminFirstName { get; set; } = "Group";
        public string AdminLastName { get; set; } = "Admin";
    }

    public class UpdateSchoolGroupInput
    {
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }

    public class SetSchoolGroupStatusRequest
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
}
