using Application.Common.Constants;
using Infrastructure.Tenancy;
using Microsoft.Extensions.Options;

namespace API.Middlewares
{
    /// <summary>
    /// Picks the tenant (school group) of each request, after authentication:
    ///   - tenant user : the "tenant_id" claim of the JWT. The X-Tenant header is ignored, so a user can
    ///                   never reach another tenant's database. Inactive tenants get 403.
    ///   - Super Admin : no tenant, unless the X-Tenant header names one (to support a school).
    ///   - anonymous   : no tenant (login, password reset only touch the catalog).
    /// </summary>
    public class TenantResolutionMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantResolutionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, TenantContext tenantContext, ITenantStore tenantStore, IOptions<TenancyOptions> options)
        {
            var user = context.User;
            if (user.Identity?.IsAuthenticated == true)
            {
                var claim = user.FindFirst(TenantClaimTypes.TenantId)?.Value;
                if (claim != null)
                {
                    var tenant = Guid.TryParse(claim, out var tenantId)
                        ? await tenantStore.FindByIdAsync(tenantId, context.RequestAborted)
                        : null;
                    if (tenant is not { IsActive: true })
                    {
                        await RejectAsync(context, StatusCodes.Status403Forbidden, "This school's account is inactive or no longer exists.");
                        return;
                    }
                    tenantContext.SetTenant(tenant);
                }
                else if (user.IsInRole(PlatformRoles.SuperAdmin)
                         && context.Request.Headers.TryGetValue(options.Value.TenantHeader, out var header)
                         && !string.IsNullOrWhiteSpace(header))
                {
                    var tenant = await tenantStore.FindByIdentifierAsync(header.ToString(), context.RequestAborted);
                    if (tenant == null)
                    {
                        await RejectAsync(context, StatusCodes.Status404NotFound, $"Unknown tenant '{header}'.");
                        return;
                    }
                    tenantContext.SetTenant(tenant);
                }
            }

            await _next(context);
        }

        private static Task RejectAsync(HttpContext context, int statusCode, string message)
        {
            context.Response.StatusCode = statusCode;
            return context.Response.WriteAsJsonAsync(new { message }, context.RequestAborted);
        }
    }
}
