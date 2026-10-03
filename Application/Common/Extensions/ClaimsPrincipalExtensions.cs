
namespace Application.Common.Extensions
{
    using System.Security.Claims;
    using System.IdentityModel.Tokens.Jwt; // or Microsoft.IdentityModel.JsonWebTokens

    public static class ClaimsPrincipalExtensions
    {
        public static string? GetUserId(this ClaimsPrincipal principal)
            => principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        public static string? GetUserEmail(this ClaimsPrincipal principal)
            => principal?.FindFirst(ClaimTypes.Email)?.Value
               ?? principal?.FindFirst(JwtRegisteredClaimNames.Email)?.Value
               ?? principal?.FindFirst("email")?.Value;
    }
}

