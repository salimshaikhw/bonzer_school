using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Infrastructure.Persistence.Catalog;
using Infrastructure.Security.Entities;
using Infrastructure.Tenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Security.Token;

public class JwtTokenService : IJwtTokenService
{

    private readonly JWTConfiguration _config;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly CatalogDbContext _dbContext;

    public JwtTokenService(
        IOptions<JWTConfiguration> options,
        UserManager<ApplicationUser> userManager,
        CatalogDbContext dbContext)
    {
        _config = options.Value;
        _userManager = userManager;
        _dbContext = dbContext;
    }

    public async Task<(string AccessToken, string RefreshToken)> GenerateTokensAsync(ApplicationUser user)
    {
        if (_config == null || string.IsNullOrEmpty(_config.Key) || string.IsNullOrEmpty(_config.Issuer) || string.IsNullOrEmpty(_config.Audience))
            throw new InvalidOperationException("JWT configuration is missing or invalid.");
        if (user == null)
            throw new ArgumentNullException(nameof(user), "User cannot be null.");

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // The tenant claim is what the API uses to pick the tenant database; never taken from the request.
        if (user.TenantId is { } tenantId)
        {
            var identifier = await _dbContext.SchoolGroups.Where(t => t.Id == tenantId).Select(t => t.Identifier).FirstOrDefaultAsync().ConfigureAwait(false);
            claims.Add(new Claim(TenantClaimTypes.TenantId, tenantId.ToString()));
            claims.Add(new Claim(TenantClaimTypes.Tenant, identifier ?? string.Empty));
        }

        var userClaims = await _userManager.GetClaimsAsync(user).ConfigureAwait(false);
        claims.AddRange(userClaims);

        var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: _config.Issuer,
            audience: _config.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_config.TokenExpiryDurationInMinutes),
            signingCredentials: creds);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);

        var refreshToken = await CreateRefreshTokenAsync(user.Id).ConfigureAwait(false);

        return (accessToken, refreshToken);
    }

    /// <summary>
    /// Two browser tabs may refresh at the same moment with the same token. A rotated token presented
    /// again within this window is refused without treating it as theft.
    /// </summary>
    private static readonly TimeSpan ReuseGracePeriod = TimeSpan.FromSeconds(30);

    public async Task<ApplicationUser?> RedeemRefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
            return null;

        var hash = Hash(refreshToken);
        var entry = await _dbContext.RefreshTokens.AsNoTracking().Include(t => t.User)
            .SingleOrDefaultAsync(t => t.TokenHash == hash)
            .ConfigureAwait(false);
        if (entry == null)
            return null;

        var now = DateTime.UtcNow;
        if (entry.Revoked != null)
        {
            if (entry.RevokedReason == RefreshTokenRevokeReasons.Rotated && entry.Revoked < now - ReuseGracePeriod)
                await RevokeAllRefreshTokensAsync(entry.UserId, RefreshTokenRevokeReasons.ReuseDetected).ConfigureAwait(false);
            return null;
        }
        if (entry.Expires < now)
            return null;

        // Conditional update, so two concurrent requests cannot both redeem the same token.
        var redeemed = await _dbContext.RefreshTokens
            .Where(t => t.Id == entry.Id && t.Revoked == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.Revoked, now)
                .SetProperty(t => t.RevokedReason, RefreshTokenRevokeReasons.Rotated))
            .ConfigureAwait(false);
        return redeemed == 1 ? entry.User : null;
    }

    public async Task<bool> RevokeRefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
            return false;

        var hash = Hash(refreshToken);
        var revoked = await _dbContext.RefreshTokens
            .Where(t => t.TokenHash == hash && t.Revoked == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.Revoked, DateTime.UtcNow)
                .SetProperty(t => t.RevokedReason, RefreshTokenRevokeReasons.SignedOut))
            .ConfigureAwait(false);
        return revoked == 1;
    }

    public Task RevokeAllRefreshTokensAsync(string userId, string reason) =>
        _dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.Revoked == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.Revoked, DateTime.UtcNow)
                .SetProperty(t => t.RevokedReason, reason));

    /// <summary>Stores the hash of a new random token and returns the raw token for the client.</summary>
    private async Task<string> CreateRefreshTokenAsync(string userId)
    {
        var tokenBytes = new byte[64];
        RandomNumberGenerator.Fill(tokenBytes);
        var token = Convert.ToBase64String(tokenBytes);

        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = Hash(token),
            Created = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddDays(_config.RefreshTokenExpiryDurationInDays),
            UserId = userId
        });
        await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        return token;
    }

    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
}
