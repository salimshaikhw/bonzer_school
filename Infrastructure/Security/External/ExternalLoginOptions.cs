namespace Infrastructure.Security.External;

/// <summary>
/// "ExternalLogin" configuration section. A provider is enabled when its ClientId is set; the ClientId is
/// the OAuth client the SPA signs in with, and is the audience the ID token must carry.
/// </summary>
public sealed class ExternalLoginOptions
{
    public const string SectionName = "ExternalLogin";

    public ProviderOptions Google { get; set; } = new();
    public ProviderOptions Microsoft { get; set; } = new();

    public sealed class ProviderOptions
    {
        public string? ClientId { get; set; }
    }
}

/// <summary>Provider names, as stored in AspNetUserLogins.LoginProvider and sent by the client.</summary>
public static class ExternalLoginProviders
{
    public const string Google = "Google";
    public const string Microsoft = "Microsoft";

    /// <summary>Canonical name for a client-supplied provider, or null when it is not supported.</summary>
    public static string? Normalize(string? provider) => provider?.Trim().ToLowerInvariant() switch
    {
        "google" => Google,
        "microsoft" => Microsoft,
        _ => null
    };
}

/// <summary>Verified identity from a provider's ID token.</summary>
/// <param name="Provider">See <see cref="ExternalLoginProviders"/>.</param>
/// <param name="Subject">Stable user id at the provider ("sub"; Microsoft: "oid" within "tid").</param>
/// <param name="Email">Email, or null when the provider did not send one.</param>
/// <param name="Directory">Google Workspace domain ("hd") or Microsoft Entra tenant id ("tid"); null for consumer accounts.</param>
public sealed record ExternalIdentity(string Provider, string Subject, string? Email, string? Directory, string? DisplayName);
