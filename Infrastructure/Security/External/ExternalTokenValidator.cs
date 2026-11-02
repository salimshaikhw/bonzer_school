using System.Security.Claims;
using Application.Common;
using Application.Exceptions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Security.External;

public interface IExternalTokenValidator
{
    /// <summary>
    /// Validates an OpenID Connect ID token that the client obtained from Google or Microsoft (signature,
    /// issuer, audience = our ClientId, lifetime) and returns who it identifies. Throws a validation
    /// <see cref="BaseServiceException"/> when the provider is unknown or disabled, or the token is invalid.
    /// </summary>
    Task<ExternalIdentity> ValidateAsync(string provider, string idToken, CancellationToken cancellationToken = default);
}

/// <summary>
/// Singleton: the providers' signing keys are fetched from their discovery documents and cached by
/// <see cref="ConfigurationManager{T}"/> (refreshed daily, or straight away when a token uses an unknown key).
/// </summary>
public sealed class ExternalTokenValidator : IExternalTokenValidator
{
    private const string GoogleMetadata = "https://accounts.google.com/.well-known/openid-configuration";
    private static readonly string[] GoogleIssuers = { "https://accounts.google.com", "accounts.google.com" };

    // "common" metadata serves the keys of every Entra directory; the issuer then names the user's directory.
    private const string MicrosoftMetadata = "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration";

    private readonly ExternalLoginOptions _options;
    private readonly JsonWebTokenHandler _handler = new();
    private readonly ConfigurationManager<OpenIdConnectConfiguration> _google = CreateManager(GoogleMetadata);
    private readonly ConfigurationManager<OpenIdConnectConfiguration> _microsoft = CreateManager(MicrosoftMetadata);

    public ExternalTokenValidator(IOptions<ExternalLoginOptions> options)
    {
        _options = options.Value;
    }

    public async Task<ExternalIdentity> ValidateAsync(string provider, string idToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idToken))
            throw Invalid("The ID token is required.");

        return ExternalLoginProviders.Normalize(provider) switch
        {
            ExternalLoginProviders.Google => await ValidateGoogleAsync(idToken, cancellationToken),
            ExternalLoginProviders.Microsoft => await ValidateMicrosoftAsync(idToken, cancellationToken),
            _ => throw Invalid($"Unsupported sign-in provider '{provider}'. Use 'google' or 'microsoft'.")
        };
    }

    private async Task<ExternalIdentity> ValidateGoogleAsync(string idToken, CancellationToken cancellationToken)
    {
        var clientId = _options.Google.ClientId;
        if (string.IsNullOrWhiteSpace(clientId))
            throw Invalid("Google sign-in is not enabled.");

        var claims = await ValidateAsync(_google, idToken, new TokenValidationParameters
        {
            ValidIssuers = GoogleIssuers,
            ValidAudience = clientId,
        }, cancellationToken);

        // Google only vouches for the address when email_verified is true.
        var email = string.Equals(claims.FindFirst("email_verified")?.Value, "true", StringComparison.OrdinalIgnoreCase) ? claims.FindFirst("email")?.Value : null;
        return new ExternalIdentity(
            ExternalLoginProviders.Google,
            Required(claims, "sub"),
            email,
            claims.FindFirst("hd")?.Value?.ToLowerInvariant(),
            claims.FindFirst("name")?.Value);
    }

    private async Task<ExternalIdentity> ValidateMicrosoftAsync(string idToken, CancellationToken cancellationToken)
    {
        var clientId = _options.Microsoft.ClientId;
        if (string.IsNullOrWhiteSpace(clientId))
            throw Invalid("Microsoft sign-in is not enabled.");

        var claims = await ValidateAsync(_microsoft, idToken, new TokenValidationParameters
        {
            ValidAudience = clientId,
            IssuerValidator = MicrosoftIssuerValidator,
        }, cancellationToken);

        // Entra does not verify "email"; it is only trusted when the tenant pins the directory (tid).
        var email = claims.FindFirst("email")?.Value ?? claims.FindFirst("preferred_username")?.Value;
        return new ExternalIdentity(
            ExternalLoginProviders.Microsoft,
            Required(claims, "sub"),
            email != null && email.Contains('@') ? email : null,
            claims.FindFirst("tid")?.Value?.ToLowerInvariant(),
            claims.FindFirst("name")?.Value);
    }

    /// <summary>The issuer must be the v2.0 issuer of the directory named in the token's own "tid" claim.</summary>
    private static string MicrosoftIssuerValidator(string issuer, SecurityToken token, TokenValidationParameters parameters)
    {
        var tid = (token as JsonWebToken)?.TryGetClaim("tid", out var claim) == true ? claim.Value : null;
        if (tid != null && Guid.TryParse(tid, out _)
            && string.Equals(issuer, $"https://login.microsoftonline.com/{tid}/v2.0", StringComparison.OrdinalIgnoreCase))
            return issuer;
        throw new SecurityTokenInvalidIssuerException($"Issuer '{issuer}' does not match the token's directory.");
    }

    private async Task<ClaimsIdentity> ValidateAsync(
        ConfigurationManager<OpenIdConnectConfiguration> manager, string idToken,
        TokenValidationParameters parameters, CancellationToken cancellationToken)
    {
        parameters.ValidateIssuerSigningKey = true;
        parameters.ValidateLifetime = true;
        parameters.ClockSkew = TimeSpan.FromMinutes(2);

        var result = await ValidateWithKeysAsync(manager, idToken, parameters, cancellationToken);
        if (result.Exception is SecurityTokenSignatureKeyNotFoundException)
        {
            // The provider rotated its keys since we last fetched them.
            manager.RequestRefresh();
            result = await ValidateWithKeysAsync(manager, idToken, parameters, cancellationToken);
        }

        if (!result.IsValid)
            throw Invalid("The sign-in token is invalid or has expired.");
        return result.ClaimsIdentity;
    }

    private async Task<TokenValidationResult> ValidateWithKeysAsync(
        ConfigurationManager<OpenIdConnectConfiguration> manager, string idToken,
        TokenValidationParameters parameters, CancellationToken cancellationToken)
    {
        var configuration = await manager.GetConfigurationAsync(cancellationToken);
        parameters.IssuerSigningKeys = configuration.SigningKeys;
        return await _handler.ValidateTokenAsync(idToken, parameters);
    }

    private static ConfigurationManager<OpenIdConnectConfiguration> CreateManager(string metadataAddress) =>
        new(metadataAddress, new OpenIdConnectConfigurationRetriever(), new HttpDocumentRetriever { RequireHttps = true });

    private static string Required(ClaimsIdentity claims, string type) =>
        claims.FindFirst(type)?.Value ?? throw Invalid($"The sign-in token has no '{type}' claim.");

    private static BaseServiceException Invalid(string message) => new(message, ExceptionCodes.Validation);
}
