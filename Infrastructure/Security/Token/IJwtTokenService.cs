using System.Threading.Tasks;
using Infrastructure.Security.Entities;

namespace Infrastructure.Security.Token;

public interface IJwtTokenService
{
    Task<(string AccessToken, string RefreshToken)> GenerateTokensAsync(ApplicationUser user);

    /// <summary>
    /// Consumes a refresh token and returns its login, or null when the token is unknown, expired or
    /// already used. Presenting an already-rotated token again revokes every token of that login.
    /// </summary>
    Task<ApplicationUser?> RedeemRefreshTokenAsync(string refreshToken);

    Task<bool> RevokeRefreshTokenAsync(string refreshToken);

    /// <summary>Ends every session of a login (password change, deactivation, suspected theft).</summary>
    Task RevokeAllRefreshTokensAsync(string userId, string reason);
}
