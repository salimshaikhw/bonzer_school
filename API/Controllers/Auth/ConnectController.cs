using System.Collections.Immutable;
using System.Security.Claims;
using System.Text.Json;
using Infrastructure.Security.OAuth;
using Infrastructure.Tenancy;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace API.Controllers.Auth;

/// <summary>
/// OAuth 2.0 token endpoint for machine clients (payment gateways, LMS, attendance devices, reporting).
/// POST /connect/token, form-encoded: grant_type=client_credentials, client_id, client_secret, scope=school_api.
/// OpenIddict has already checked the client and its secret; this issues the access token with the tenant
/// and roles stored on the client application (see OAuthClientsController). The token is then accepted by
/// every [Authorize] endpoint like a user's JWT, limited to those roles and that tenant.
/// </summary>
[ApiExplorerSettings(IgnoreApi = true)]
public class ConnectController : ControllerBase
{
    private readonly IOpenIddictApplicationManager _applications;
    private readonly ITenantStore _tenantStore;

    public ConnectController(IOpenIddictApplicationManager applications, ITenantStore tenantStore)
    {
        _applications = applications;
        _tenantStore = tenantStore;
    }

    [HttpPost("~/" + OAuthServerOptions.TokenEndpoint)]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken] // NOSONAR: OAuth token endpoint for machine clients (form + client secret, no cookies): nothing to forge
    [Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        // Null when the OAuth server is not enabled (no keys configured outside Development).
        var request = HttpContext.GetOpenIddictServerRequest();
        if (request == null)
            return NotFound();

        if (!request.IsClientCredentialsGrantType())
            return Reject(Errors.UnsupportedGrantType, "Only the client_credentials grant is supported.");

        var application = await _applications.FindByClientIdAsync(request.ClientId!);
        if (application == null)
            return Reject(Errors.InvalidClient, "Unknown client.");

        var properties = await _applications.GetPropertiesAsync(application);
        var tenant = properties.TryGetValue(OAuthServerOptions.TenantProperty, out var tenantValue)
                     && Guid.TryParse(tenantValue.GetString(), out var tenantId)
            ? await _tenantStore.FindByIdAsync(tenantId, HttpContext.RequestAborted)
            : null;
        if (tenant is not { IsActive: true })
            return Reject(Errors.InvalidClient, "The school of this client is inactive or no longer exists.");

        var roles = properties.TryGetValue(OAuthServerOptions.RolesProperty, out var rolesValue) && rolesValue.ValueKind == JsonValueKind.Array
            ? rolesValue.EnumerateArray().Select(r => r.GetString()).OfType<string>().ToImmutableArray()
            : ImmutableArray<string>.Empty;

        var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType, Claims.Name, Claims.Role);
        identity.SetClaim(Claims.Subject, request.ClientId);
        identity.SetClaim(Claims.Name, await _applications.GetDisplayNameAsync(application));
        identity.SetClaim(TenantClaimTypes.TenantId, tenant.Id.ToString());
        identity.SetClaim(TenantClaimTypes.Tenant, tenant.Identifier);
        identity.SetClaims(Claims.Role, roles);

        identity.SetScopes(request.GetScopes());
        identity.SetResources(OAuthServerOptions.ApiAudience);
        identity.SetDestinations(_ => new[] { Destinations.AccessToken });

        return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private IActionResult Reject(string error, string description) =>
        Forbid(new AuthenticationProperties(new Dictionary<string, string?>
        {
            [OpenIddictServerAspNetCoreConstants.Properties.Error] = error,
            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description
        }), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
}
