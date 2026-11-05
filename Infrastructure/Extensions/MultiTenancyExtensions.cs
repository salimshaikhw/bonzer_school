using Application.Abstractions.Tenancy;
using Infrastructure.Persistence.Catalog;
using Infrastructure.Persistence.Interceptors;
using Infrastructure.Persistence.Postgres;
using Infrastructure.Services.Organization;
using Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Extensions;

public static class MultiTenancyExtensions
{
    /// <summary>
    /// Database-per-tenant setup:
    ///   CatalogDbContext      -> the shared catalog database (tenants, logins, shared lookups)
    ///   BonzerSchoolDbContext -> the database of the request's tenant (ITenantContext), chosen when the
    ///                            context is created; without a tenant, any query fails with a clear error.
    /// The tenant is set by the API's TenantResolutionMiddleware from the "tenant_id" JWT claim.
    /// </summary>
    public static IServiceCollection AddMultiTenancy(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TenancyOptions>(configuration.GetSection(TenancyOptions.SectionName));

        services.AddSingleton<TenantConnectionStrings>();
        services.AddSingleton<TenantDbContextFactory>();
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
        services.AddScoped<ITenantStore, TenantStore>();
        services.AddScoped<ISchoolGroupService, SchoolGroupService>();

        // Fills CreatedAt/CreatedBy/UpdatedAt/UpdatedBy on every SaveChanges (see AuditableEntity).
        services.AddSingleton<AuditableEntityInterceptor>();

        services.AddDbContext<CatalogDbContext>((sp, options) =>
            options.UseNpgsql(sp.GetRequiredService<TenantConnectionStrings>().Catalog)
                   .AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>()));

        services.AddDbContext<BonzerSchoolDbContext>((sp, options) =>
        {
            var tenant = sp.GetRequiredService<ITenantContext>().Current;
            if (tenant != null)
                options.UseNpgsql(sp.GetRequiredService<TenantConnectionStrings>().ForDatabase(tenant.DatabaseName));
            else
                options.UseNpgsql().AddInterceptors(TenantRequiredInterceptor.Instance);

            options.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
        });

        return services;
    }
}
