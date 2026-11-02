using Application.Abstractions.Tenancy;
using Application.Common.Constants;
using Application.ViewModels.Tenancy;
using Infrastructure.Persistence.Catalog;
using Infrastructure.Security.Entities;
using Infrastructure.Tenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Persistence.Seed;

/// <summary>
/// Runs at startup:
///   catalog     : create/upgrade the catalog database, seed Identity roles, the shared lookups
///                 (institution types, mediums) and the platform Super Admin (PlatformAdmin:Email/Password)
///   demo (dev)  : provision the demo tenant (Seed:DemoTenant, default "demo") with its Group Admin
///                 (Admin:Email/Password), one login per role and a demo school (see DemoDataSeeder)
///   tenants     : apply pending migrations + reference data to every active tenant database
///                 (Tenancy:MigrateTenantsOnStartup)
/// Every step checks what exists first, so it is safe on every restart.
/// </summary>
public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, bool seedDemoData)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var catalog = sp.GetRequiredService<CatalogDbContext>();
        var config = sp.GetRequiredService<IConfiguration>();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DatabaseInitializer));

        // Creates the catalog database if it does not exist and applies pending migrations.
        await catalog.Database.MigrateAsync();
        logger.LogInformation("Catalog database is up to date ({Count} migrations applied).",
            (await catalog.Database.GetAppliedMigrationsAsync()).Count());

        await SeedRolesAsync(sp.GetRequiredService<RoleManager<IdentityRole>>());
        await SeedCatalogLookupsAsync(catalog);

        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var platformEmail = config["PlatformAdmin:Email"] ?? "superadmin@bonzer.local";
        var platformPassword = config["PlatformAdmin:Password"];
        if (string.IsNullOrWhiteSpace(platformPassword))
            logger.LogWarning("No Super Admin seeded: PlatformAdmin:Password is not configured.");
        else
            await EnsureUserAsync(userManager, logger, platformEmail, platformPassword, "Super", "Admin", PlatformRoles.SuperAdmin, tenantId: null);

        var tenantService = sp.GetRequiredService<ITenantService>();
        if (sp.GetRequiredService<IOptions<TenancyOptions>>().Value.MigrateTenantsOnStartup)
            await tenantService.MigrateAllAsync();

        if (seedDemoData)
            await SeedDemoTenantAsync(sp, config, logger, catalog, userManager, tenantService);
    }

    private static async Task SeedDemoTenantAsync(
        IServiceProvider sp, IConfiguration config, ILogger logger,
        CatalogDbContext catalog, UserManager<ApplicationUser> userManager, ITenantService tenantService)
    {
        var demoPassword = config["DemoUsers:Password"];
        var groupAdminEmail = config["Admin:Email"] ?? "groupadmin@bonzer.local";
        var groupAdminPassword = config["Admin:Password"];
        if (string.IsNullOrWhiteSpace(demoPassword) || string.IsNullOrWhiteSpace(groupAdminPassword))
        {
            logger.LogWarning("Demo data skipped: DemoUsers:Password and Admin:Password must both be configured.");
            return;
        }

        var identifier = config["Seed:DemoTenant"] ?? "demo";
        var tenant = await catalog.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Identifier == identifier);
        if (tenant == null)
        {
            await tenantService.CreateAsync(new CreateTenantRequest
            {
                Identifier = identifier,
                Name = "Bonzer Education Trust",
                Code = "BET",
                Address = "12 MG Road, Pune",
                Phone = "020-24000000",
                Email = "info@bonzer.local",
                AdminEmail = groupAdminEmail,
                AdminPassword = groupAdminPassword,
            });
            tenant = await catalog.Tenants.AsNoTracking().FirstAsync(t => t.Identifier == identifier);
        }

        var info = new TenantInfo(tenant.Id, tenant.Identifier, tenant.Name, tenant.DatabaseName, tenant.IsActive);
        await using var db = sp.GetRequiredService<TenantDbContextFactory>().Create(info);
        await DemoDataSeeder.SeedAsync(db, catalog, userManager, logger, demoPassword, tenant.Id);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in Roles.All.Concat(PlatformRoles.All))
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    /// <summary>Lookups shared by every tenant (catalog tables).</summary>
    private static async Task SeedCatalogLookupsAsync(CatalogDbContext catalog)
    {
        await AddMissingAsync(catalog, x => x.Name,
            new InstitutionType { Name = "Pre-Primary" },
            new InstitutionType { Name = "School" },
            new InstitutionType { Name = "Junior College" });

        await AddMissingAsync(catalog, x => x.Name,
            new Medium { Name = "English" },
            new Medium { Name = "Semi-English" },
            new Medium { Name = "Marathi" },
            new Medium { Name = "Hindi" });
    }

    /// <summary>Adds the items whose key (e.g. Name) is not in the table yet.</summary>
    internal static async Task AddMissingAsync<T>(DbContext db, Func<T, string?> key, params T[] items) where T : class
    {
        var set = db.Set<T>();
        var existing = (await set.ToListAsync()).Select(key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items.Where(i => !existing.Contains(key(i) ?? "")))
            set.Add(item);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Creates the login if needed and gives it the role. A login belongs to one tenant: an existing
    /// login of another tenant is left alone.
    /// </summary>
    internal static async Task<ApplicationUser?> EnsureUserAsync(
        UserManager<ApplicationUser> userManager, ILogger logger,
        string email, string password, string firstName, string lastName, string role, Guid? tenantId)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                TenantId = tenantId,
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName,
                DisplayName = $"{firstName} {lastName}",
                CreatedOn = DateTime.UtcNow,
                CreatedBy = "seed",
            };
            var created = await userManager.CreateAsync(user, password);
            if (!created.Succeeded)
            {
                logger.LogWarning("Seed: could not create {Email}: {Errors}", email, string.Join("; ", created.Errors.Select(e => e.Description)));
                return null;
            }
        }
        else if (user.TenantId != tenantId)
        {
            logger.LogWarning("Seed: {Email} already belongs to another tenant; skipped.", email);
            return null;
        }

        if (!await userManager.IsInRoleAsync(user, role))
            await userManager.AddToRoleAsync(user, role);
        return user;
    }
}
