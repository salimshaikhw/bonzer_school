using Infrastructure.Persistence.Conventions;
using Infrastructure.Security.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Catalog;

/// <summary>
/// The shared ("base") database, one per deployment. Holds what is common to every tenant:
///   - organizations: the registry of school groups (= tenants) and the name of each one's database
///   - ASP.NET Identity (logins, roles) and refresh tokens; each login carries its TenantId
///   - shared lookups: institution_types, mediums
///   - OAuth 2.0 server (OpenIddict*): client applications of integrations, each bound to one tenant,
///     plus the authorizations and tokens OpenIddict keeps for them
/// Each tenant's school data lives in its own database, see <see cref="Postgres.BonzerSchoolDbContext"/>.
/// Migrations: Persistence/Migrations/Catalog. Mapping follows the same annotation and snake_case rules
/// as the tenant context.
/// </summary>
public class CatalogDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<SchoolGroup> SchoolGroups { get; set; }
    public virtual DbSet<InstitutionType> InstitutionTypes { get; set; }
    public virtual DbSet<Medium> Mediums { get; set; }
    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyDomainAnnotations();

        // OpenIddict entities keep OpenIddict's own table and column names, like the AspNet* tables.
        modelBuilder.UseOpenIddict();

        // ApplicationUser is an Identity type (not in Domain.Entities), so the [References] convention
        // does not reach it.
        modelBuilder.Entity<ApplicationUser>(user =>
        {
            user.HasIndex(u => u.TenantId);
            user.HasOne<SchoolGroup>().WithMany().HasForeignKey(u => u.TenantId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder.Properties<DateTime>().HaveColumnType("timestamp without time zone");
        configurationBuilder.Properties<DateTime?>().HaveColumnType("timestamp without time zone");

        configurationBuilder.Conventions.Add(_ => new SnakeCaseNamingConvention());
    }
}
