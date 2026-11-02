using System.Security.Cryptography;
using System.Text.Json;
using Application.Abstractions.Tenancy;
using Application.Common.Constants;
using Infrastructure.Security.OAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace API.Controllers.Auth;

/// <summary>
/// OAuth 2.0 clients (integrations) of the current tenant: a Group Admin manages their own school group's
/// clients; the Super Admin does it for any tenant by sending X-Tenant. Each client gets a client id and
/// secret for the client_credentials grant at POST /connect/token, and acts with the school roles given here.
/// </summary>
[ApiController]
[Route("api/oauth-clients")]
[Authorize(Roles = RoleGroups.PlatformAdmin + "," + RoleGroups.GroupAdminOnly)]
public class OAuthClientsController : ControllerBase
{
    private readonly IOpenIddictApplicationManager _applications;
    private readonly ITenantContext _tenantContext;

    public OAuthClientsController(IOpenIddictApplicationManager applications, ITenantContext tenantContext)
    {
        _applications = applications;
        _tenantContext = tenantContext;
    }

    public sealed class CreateOAuthClientRequest
    {
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>School roles (see Roles), e.g. ["Accountant"] for a payment gateway. GroupAdmin cannot be granted.</summary>
        public List<string> Roles { get; set; } = new();
    }

    public sealed record OAuthClientResponse(string ClientId, string? DisplayName, IReadOnlyList<string> Roles);

    /// <summary>The secret is only returned here; it is stored hashed and cannot be read again.</summary>
    public sealed record CreatedOAuthClientResponse(string ClientId, string ClientSecret, string? DisplayName, IReadOnlyList<string> Roles, string TokenEndpoint, string Scope);

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        if (_tenantContext.Current is not { } tenant)
            return BadRequest("Select a tenant (X-Tenant header).");

        var clients = new List<OAuthClientResponse>();
        await foreach (var application in _applications.ListAsync(cancellationToken: cancellationToken))
        {
            var properties = await _applications.GetPropertiesAsync(application, cancellationToken);
            if (TenantOf(properties) != tenant.Id)
                continue;
            clients.Add(new OAuthClientResponse(
                (await _applications.GetClientIdAsync(application, cancellationToken))!,
                await _applications.GetDisplayNameAsync(application, cancellationToken),
                RolesOf(properties)));
        }
        return Ok(clients);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOAuthClientRequest request, CancellationToken cancellationToken)
    {
        if (_tenantContext.Current is not { } tenant)
            return BadRequest("Select a tenant (X-Tenant header).");
        if (string.IsNullOrWhiteSpace(request.DisplayName))
            return BadRequest("DisplayName is required.");

        var roles = request.Roles.Select(r => r.Trim()).Distinct(StringComparer.Ordinal).ToList();
        var invalid = roles.Where(r => !Roles.All.Contains(r) || r == Roles.GroupAdmin).ToList();
        if (roles.Count == 0 || invalid.Count > 0)
            return BadRequest($"Give at least one school role. Not allowed: {string.Join(", ", invalid)}.");

        var clientId = $"{tenant.Identifier}-{Convert.ToHexString(RandomNumberGenerator.GetBytes(6)).ToLowerInvariant()}";
        var clientSecret = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            ClientType = ClientTypes.Confidential,
            DisplayName = request.DisplayName.Trim(),
            Permissions =
            {
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.ClientCredentials,
                Permissions.Prefixes.Scope + OAuthServerOptions.ApiScope
            }
        };
        descriptor.Properties[OAuthServerOptions.TenantProperty] = JsonSerializer.SerializeToElement(tenant.Id.ToString());
        descriptor.Properties[OAuthServerOptions.RolesProperty] = JsonSerializer.SerializeToElement(roles);

        await _applications.CreateAsync(descriptor, cancellationToken);

        return Ok(new CreatedOAuthClientResponse(clientId, clientSecret, descriptor.DisplayName, roles,
            "/" + OAuthServerOptions.TokenEndpoint, OAuthServerOptions.ApiScope));
    }

    /// <summary>The client can no longer get tokens; tokens already issued expire within their lifetime.</summary>
    [HttpDelete("{clientId}")]
    public async Task<IActionResult> Delete(string clientId, CancellationToken cancellationToken)
    {
        if (_tenantContext.Current is not { } tenant)
            return BadRequest("Select a tenant (X-Tenant header).");

        var application = await _applications.FindByClientIdAsync(clientId, cancellationToken);
        if (application == null || TenantOf(await _applications.GetPropertiesAsync(application, cancellationToken)) != tenant.Id)
            return NotFound();

        await _applications.DeleteAsync(application, cancellationToken);
        return NoContent();
    }

    private static Guid? TenantOf(IReadOnlyDictionary<string, JsonElement> properties) =>
        properties.TryGetValue(OAuthServerOptions.TenantProperty, out var value) && Guid.TryParse(value.GetString(), out var id)
            ? id
            : null;

    private static IReadOnlyList<string> RolesOf(IReadOnlyDictionary<string, JsonElement> properties) =>
        properties.TryGetValue(OAuthServerOptions.RolesProperty, out var value) && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray().Select(r => r.GetString()).OfType<string>().ToList()
            : Array.Empty<string>();
}
