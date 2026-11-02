using Application.Abstractions.Tenancy;
using Infrastructure.Persistence.Interceptors;
using Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tenancy;

/// <summary>
/// Opens a tenant database outside the request pipeline: provisioning, startup migrations, seeding.
/// Request code gets BonzerSchoolDbContext from DI instead, already pointed at the current tenant.
/// The caller disposes the context.
/// </summary>
public sealed class TenantDbContextFactory(TenantConnectionStrings connectionStrings, AuditableEntityInterceptor auditInterceptor)
{
    public BonzerSchoolDbContext Create(TenantInfo tenant)
    {
        var options = new DbContextOptionsBuilder<BonzerSchoolDbContext>()
            .UseNpgsql(connectionStrings.ForDatabase(tenant.DatabaseName))
            .AddInterceptors(auditInterceptor)
            .Options;
        return new BonzerSchoolDbContext(options);
    }
}
