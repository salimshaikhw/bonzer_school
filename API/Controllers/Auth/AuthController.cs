using System;
using System.Threading;
using System.Threading.Tasks;
using Infrastructure.Persistence.Catalog;
using Infrastructure.Security.Entities;
using Infrastructure.Security.External;
using Infrastructure.Security.Token;
using Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace API.Controllers.Auth;

/// <summary>
/// Sign-in for people (the SPA). Every way in ends with the same token pair from <see cref="IJwtTokenService"/>:
///   - email + password                       POST login
///   - Google / Microsoft ID token (OIDC)     POST external
/// Machine clients (integrations) use the OAuth 2.0 token endpoint instead, see ConnectController.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    /// <summary>Same answer for every failed external sign-in, so it does not reveal which emails have a login.</summary>
    private const string ExternalLoginNotLinked =
        "No school account is linked to this sign-in. Sign in with your password once and link it from your profile, or ask your school administrator.";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _tokenService;
    private readonly CatalogDbContext _db;
    private readonly ITenantStore _tenantStore;
    private readonly IExternalTokenValidator _externalTokenValidator;
    private readonly ExternalLoginOptions _externalLoginOptions;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        IJwtTokenService tokenService,
        CatalogDbContext db,
        ITenantStore tenantStore,
        IExternalTokenValidator externalTokenValidator,
        IOptions<ExternalLoginOptions> externalLoginOptions)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _db = db;
        _tenantStore = tenantStore;
        _externalTokenValidator = externalTokenValidator;
        _externalLoginOptions = externalLoginOptions.Value;
    }

    /// <summary>Logins of a deactivated school group cannot sign in. Platform users have no tenant.</summary>
    private async Task<bool> TenantIsActiveAsync(ApplicationUser user)
    {
        if (user.TenantId is not { } tenantId)
            return true;
        var tenant = await _tenantStore.FindByIdAsync(tenantId);
        return tenant is { IsActive: true };
    }

    /// <summary>JSON: { "email", "password" } (camelCase). Prefer class binding over positional records for broad serializer compatibility.</summary>
    public sealed class LoginRequest
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
    }

    public record RefreshRequest(string RefreshToken);

    /// <summary>JSON: { "provider": "google" | "microsoft", "idToken": "..." }.</summary>
    public sealed class ExternalLoginRequest
    {
        public string? Provider { get; set; }

        /// <summary>The OpenID Connect ID token the SPA received from the provider (not its access token).</summary>
        public string? IdToken { get; set; }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest? req)
    {
        var email = req?.Email?.Trim();
        var password = req?.Password;
        if (string.IsNullOrEmpty(email) || password is null)
            return BadRequest("Email and password are required.");

        // Identity matches by normalized email; trailing spaces in UI often break lookup.
        var user = await _userManager.FindByEmailAsync(email)
            ?? await _userManager.FindByNameAsync(email);

        if (user == null)
            return Unauthorized("Invalid email or password.");

        if (await _userManager.IsLockedOutAsync(user))
            return Unauthorized("Account is locked after too many failed sign-ins. Try again later or reset your password.");

        if (!await _userManager.CheckPasswordAsync(user, password))
        {
            // Counts towards Identity's lockout (5 failures -> 5 minutes by default).
            await _userManager.AccessFailedAsync(user);
            return Unauthorized("Invalid email or password.");
        }
        await _userManager.ResetAccessFailedCountAsync(user);

        return await IssueTokensAsync(user);
    }

    /// <summary>Lists the enabled SSO providers and the client ids the SPA must sign in with.</summary>
    [HttpGet("external/providers")]
    [AllowAnonymous]
    public IActionResult ExternalProviders()
    {
        return Ok(new
        {
            google = string.IsNullOrWhiteSpace(_externalLoginOptions.Google.ClientId) ? null : new { clientId = _externalLoginOptions.Google.ClientId },
            microsoft = string.IsNullOrWhiteSpace(_externalLoginOptions.Microsoft.ClientId) ? null : new { clientId = _externalLoginOptions.Microsoft.ClientId },
        });
    }

    /// <summary>
    /// Signs in with a Google or Microsoft ID token. The account must already exist (created by the school):
    /// it is found by a previously linked provider account, or, the first time, by email when the token
    /// comes from the Google Workspace domain / Microsoft Entra directory configured for the user's tenant.
    /// </summary>
    [HttpPost("external")]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalLogin([FromBody] ExternalLoginRequest req, CancellationToken cancellationToken)
    {
        var identity = await _externalTokenValidator.ValidateAsync(req.Provider ?? string.Empty, req.IdToken ?? string.Empty, cancellationToken);

        var user = await _userManager.FindByLoginAsync(identity.Provider, identity.Subject)
            ?? await LinkByTrustedEmailAsync(identity, cancellationToken);
        if (user == null)
            return Unauthorized(ExternalLoginNotLinked);

        if (await _userManager.IsLockedOutAsync(user))
            return Unauthorized("Account is locked after too many failed sign-ins. Try again later or reset your password.");

        return await IssueTokensAsync(user);
    }

    /// <summary>Links a Google or Microsoft account to the signed-in login, so it can be used to sign in.</summary>
    [HttpPost("external/link")]
    [Authorize]
    public async Task<IActionResult> LinkExternalLogin([FromBody] ExternalLoginRequest req, CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return BadRequest("Only personal logins can link a sign-in provider.");

        var identity = await _externalTokenValidator.ValidateAsync(req.Provider ?? string.Empty, req.IdToken ?? string.Empty, cancellationToken);

        var owner = await _userManager.FindByLoginAsync(identity.Provider, identity.Subject);
        if (owner != null)
            return owner.Id == user.Id ? NoContent() : Conflict("This account is already linked to another login.");

        var result = await _userManager.AddLoginAsync(user, new UserLoginInfo(identity.Provider, identity.Subject, identity.Provider));
        return result.Succeeded ? NoContent() : BadRequest(result.Errors);
    }

    [HttpDelete("external/{provider}")]
    [Authorize]
    public async Task<IActionResult> UnlinkExternalLogin(string provider)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return BadRequest("Only personal logins can unlink a sign-in provider.");

        var name = ExternalLoginProviders.Normalize(provider);
        var logins = await _userManager.GetLoginsAsync(user);
        foreach (var login in logins)
        {
            if (login.LoginProvider != name)
                continue;
            var result = await _userManager.RemoveLoginAsync(user, login.LoginProvider, login.ProviderKey);
            if (!result.Succeeded)
                return BadRequest(result.Errors);
        }
        return NoContent();
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest req)
    {
        // Single use: the token is consumed here and a new pair is issued below.
        var user = await _tokenService.RedeemRefreshTokenAsync(req.RefreshToken);
        if (user == null)
            return BadRequest("Invalid or expired refresh token");

        if (await _userManager.IsLockedOutAsync(user))
            return Unauthorized("Account is locked.");

        return await IssueTokensAsync(user);
    }

    [HttpPost("revoke")]
    [AllowAnonymous]
    public async Task<IActionResult> Revoke([FromBody] RefreshRequest req)
    {
        var revoked = await _tokenService.RevokeRefreshTokenAsync(req.RefreshToken);
        if (!revoked) return BadRequest("Token not found or already revoked");
        return NoContent();
    }

    /// <summary>
    /// First external sign-in: matches the login by email, but only when the token comes from the directory
    /// the user's tenant trusts (Google "hd" / Microsoft "tid"). Consumer accounts and other directories can
    /// set any email, so they are never matched; their owners link them explicitly instead.
    /// </summary>
    private async Task<ApplicationUser?> LinkByTrustedEmailAsync(ExternalIdentity identity, CancellationToken cancellationToken)
    {
        if (identity.Email == null || identity.Directory == null)
            return null;

        var user = await _userManager.FindByEmailAsync(identity.Email);
        if (user?.TenantId is not { } tenantId)
            return null; // no login, or a platform user (must link explicitly)

        var tenant = await _db.SchoolGroups.AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(t => new { t.GoogleWorkspaceDomain, t.MicrosoftEntraTenantId })
            .FirstOrDefaultAsync(cancellationToken);
        if (tenant == null)
            return null;

        var trusted = identity.Provider switch
        {
            ExternalLoginProviders.Google =>
                tenant.GoogleWorkspaceDomain != null && tenant.GoogleWorkspaceDomain == identity.Directory,
            ExternalLoginProviders.Microsoft =>
                tenant.MicrosoftEntraTenantId is { } entraId && Guid.TryParse(identity.Directory, out var tid) && tid == entraId,
            _ => false
        };
        if (!trusted)
            return null;

        var result = await _userManager.AddLoginAsync(user, new UserLoginInfo(identity.Provider, identity.Subject, identity.Provider));
        return result.Succeeded ? user : null;
    }

    private async Task<IActionResult> IssueTokensAsync(ApplicationUser user)
    {
        if (!await TenantIsActiveAsync(user))
            return Unauthorized("This school's account is inactive. Contact support.");

        try
        {
            var (accessToken, refreshToken) = await _tokenService.GenerateTokensAsync(user);
            // Anonymous object guarantees System.Text.Json camelCase for the SPA.
            return Ok(new { accessToken, refreshToken });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "Sign-in succeeded but the server could not issue tokens. Ensure PostgreSQL is running and refresh-token tables exist.",
                detail = ex.Message,
            });
        }
    }
}
