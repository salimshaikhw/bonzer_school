using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Abstractions.Tenancy;

namespace API.Logging;

/// <summary>Who and what a request is about, as log properties (tenant, user). Values are never secrets.</summary>
public static class RequestLogContext
{
    public static IEnumerable<KeyValuePair<string, object?>> Collect(HttpContext context)
    {
        var tenant = context.RequestServices.GetService<ITenantContext>()?.Current;
        if (tenant != null)
        {
            yield return new(LogProperties.TenantId, tenant.Id);
            yield return new(LogProperties.Tenant, tenant.Identifier);
        }

        var userId = UserId(context.User);
        if (userId != null)
            yield return new(LogProperties.UserId, userId);
    }

    public static string? UserId(ClaimsPrincipal user) =>
        user.Identity?.IsAuthenticated == true
            ? user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            : null;
}
