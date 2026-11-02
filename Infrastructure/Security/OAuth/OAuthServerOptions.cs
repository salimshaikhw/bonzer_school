namespace Infrastructure.Security.OAuth;

/// <summary>
/// "OAuthServer" configuration section. Keep the keys in User Secrets / Azure configuration, never in
/// appsettings: SigningCertificate is a base64 PFX (RSA), EncryptionKey is 32 random bytes in base64.
/// </summary>
public sealed class OAuthServerOptions
{
    public const string SectionName = "OAuthServer";

    public const string TokenEndpoint = "connect/token";

    /// <summary>Scope a client must hold to call the API.</summary>
    public const string ApiScope = "school_api";

    /// <summary>Audience ("aud") of the access tokens; the API only accepts tokens issued for it.</summary>
    public const string ApiAudience = "bonzer-school-api";

    /// <summary>Application property: catalog tenants.id the client acts for.</summary>
    public const string TenantProperty = "tenant_id";

    /// <summary>Application property: JSON array of school roles granted to the client.</summary>
    public const string RolesProperty = "roles";

    public bool Enabled { get; set; } = true;
    public string? SigningCertificate { get; set; }
    public string? SigningCertificatePassword { get; set; }
    public string? EncryptionKey { get; set; }
    public int AccessTokenLifetimeMinutes { get; set; } = 60;
}
