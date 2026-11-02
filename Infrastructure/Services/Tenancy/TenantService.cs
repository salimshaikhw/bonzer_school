using System.Text.RegularExpressions;
using Application.Abstractions.Tenancy;
using Application.Common;
using Application.Common.Constants;
using Application.Exceptions;
using Application.ViewModels.Tenancy;
using Infrastructure.Persistence.Catalog;
using Infrastructure.Persistence.Seed;
using Infrastructure.Security.Entities;
using Infrastructure.Tenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Tenancy;

/// <summary>
/// Tenant lifecycle. Creating a tenant is all-or-nothing: if the database, seed or admin login fails,
/// the new database, login and catalog row are removed again.
/// </summary>
public sealed partial class TenantService(
    CatalogDbContext catalog,
    UserManager<ApplicationUser> userManager,
    TenantConnectionStrings connectionStrings,
    TenantDbContextFactory tenantDbFactory,
    ITenantStore tenantStore,
    ILogger<TenantService> logger) : ITenantService
{
    // 3-40 characters: lower-case letters, digits and inner hyphens; starts with a letter.
    [GeneratedRegex("^[a-z][a-z0-9-]{1,38}[a-z0-9]$")]
    private static partial Regex IdentifierPattern();

    public async Task<IEnumerable<TenantResponse>> GetAllAsync() =>
        await catalog.Tenants.AsNoTracking().OrderBy(t => t.Name).Select(t => ToResponse(t)).ToListAsync();

    public async Task<TenantResponse> GetByIdAsync(Guid id) => ToResponse(await FindAsync(id));

    public async Task<TenantResponse> CreateAsync(CreateTenantRequest request)
    {
        var identifier = request.Identifier?.Trim().ToLowerInvariant() ?? string.Empty;
        await ValidateAsync(request, identifier);

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Identifier = identifier,
            Name = request.Name.Trim(),
            DatabaseName = connectionStrings.DatabaseNameFor(identifier),
            IsActive = false, // switched on once everything below has succeeded
        };
        var info = ToInfo(tenant);

        await using var tenantDb = tenantDbFactory.Create(info);
        if (await tenantDb.GetService<IRelationalDatabaseCreator>().ExistsAsync())
            throw new BaseServiceException(
                $"Database '{tenant.DatabaseName}' already exists on the server. Choose another identifier.", ExceptionCodes.Validation);

        catalog.Tenants.Add(tenant);
        await catalog.SaveChangesAsync();

        ApplicationUser? admin = null;
        try
        {
            await tenantDb.Database.MigrateAsync();
            await TenantDataSeeder.SeedAsync(tenantDb, info, new SchoolGroup
            {
                Name = tenant.Name,
                Code = request.Code,
                Address = request.Address,
                Phone = request.Phone,
                Email = request.Email,
            });

            var newAdmin = new ApplicationUser
            {
                TenantId = tenant.Id,
                UserName = request.AdminEmail.Trim(),
                Email = request.AdminEmail.Trim(),
                EmailConfirmed = true,
                FirstName = request.AdminFirstName,
                LastName = request.AdminLastName,
                DisplayName = $"{request.AdminFirstName} {request.AdminLastName}",
                CreatedOn = DateTime.UtcNow,
                CreatedBy = "platform",
            };
            ThrowIfFailed(await userManager.CreateAsync(newAdmin, request.AdminPassword));
            admin = newAdmin;
            ThrowIfFailed(await userManager.AddToRoleAsync(admin, Roles.GroupAdmin));

            tenant.IsActive = true;
            await catalog.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Provisioning tenant {Tenant} failed; rolling back.", identifier);
            await RollBackProvisioningAsync(tenant, admin, tenantDb);
            throw;
        }

        logger.LogInformation("Tenant {Tenant} provisioned in database {Database}.", identifier, tenant.DatabaseName);
        return ToResponse(tenant);
    }

    public async Task<TenantResponse> SetActiveAsync(Guid id, bool isActive)
    {
        var tenant = await FindAsync(id, track: true);
        tenant.IsActive = isActive;
        await catalog.SaveChangesAsync();
        tenantStore.Invalidate(tenant.Id, tenant.Identifier);
        logger.LogInformation("Tenant {Tenant} {State}.", tenant.Identifier, isActive ? "activated" : "deactivated");
        return ToResponse(tenant);
    }

    public async Task<TenantResponse> SetExternalLoginAsync(Guid id, SetExternalLoginRequest request)
    {
        var tenant = await FindAsync(id, track: true);
        var domain = request.GoogleWorkspaceDomain?.Trim().ToLowerInvariant();
        tenant.GoogleWorkspaceDomain = string.IsNullOrEmpty(domain) ? null : domain;
        tenant.MicrosoftEntraTenantId = request.MicrosoftEntraTenantId == Guid.Empty ? null : request.MicrosoftEntraTenantId;
        await catalog.SaveChangesAsync();
        logger.LogInformation("Tenant {Tenant} external login: Google domain {Domain}, Entra tenant {Entra}.",
            tenant.Identifier, tenant.GoogleWorkspaceDomain ?? "-", tenant.MicrosoftEntraTenantId?.ToString() ?? "-");
        return ToResponse(tenant);
    }

    public async Task<IReadOnlyList<string>> MigrateAllAsync(CancellationToken cancellationToken = default)
    {
        var tenants = await catalog.Tenants.AsNoTracking().Where(t => t.IsActive).ToListAsync(cancellationToken);
        var migrated = new List<string>();
        foreach (var tenant in tenants)
        {
            try
            {
                await using var db = tenantDbFactory.Create(ToInfo(tenant));
                await db.Database.MigrateAsync(cancellationToken);
                await TenantDataSeeder.SeedAsync(db, ToInfo(tenant));
                migrated.Add(tenant.Identifier);
            }
            catch (Exception ex)
            {
                // One broken tenant database must not stop the others (or the API) from starting.
                logger.LogError(ex, "Migrating tenant {Tenant} ({Database}) failed.", tenant.Identifier, tenant.DatabaseName);
            }
        }
        logger.LogInformation("Tenant databases up to date: {Migrated}/{Total}.", migrated.Count, tenants.Count);
        return migrated;
    }

    private async Task ValidateAsync(CreateTenantRequest request, string identifier)
    {
        if (!IdentifierPattern().IsMatch(identifier))
            throw new BaseServiceException(
                "Identifier must be 3-40 characters: lower-case letters, digits and hyphens, starting with a letter.", ExceptionCodes.Validation);
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new BaseServiceException("Name is required.", ExceptionCodes.Validation);
        if (string.IsNullOrWhiteSpace(request.AdminEmail) || string.IsNullOrEmpty(request.AdminPassword))
            throw new BaseServiceException("AdminEmail and AdminPassword are required.", ExceptionCodes.Validation);

        if (await catalog.Tenants.AnyAsync(t => t.Identifier == identifier))
            throw new BaseServiceException($"Tenant '{identifier}' already exists.", ExceptionCodes.Validation);
        if (await userManager.FindByEmailAsync(request.AdminEmail.Trim()) != null)
            throw new BaseServiceException($"Email {request.AdminEmail} is already registered.", ExceptionCodes.Validation);

        // Check the password before any database is created.
        foreach (var validator in userManager.PasswordValidators)
            ThrowIfFailed(await validator.ValidateAsync(userManager, new ApplicationUser(), request.AdminPassword));
    }

    private async Task RollBackProvisioningAsync(Tenant tenant, ApplicationUser? admin, DbContext tenantDb)
    {
        try
        {
            if (admin != null)
                await userManager.DeleteAsync(admin);
        }
        catch (Exception ex) { logger.LogError(ex, "Rollback: could not delete login {Email}.", admin?.Email); }

        try
        {
            // Safe: CreateAsync checked that this database did not exist before.
            await tenantDb.Database.EnsureDeletedAsync();
        }
        catch (Exception ex) { logger.LogError(ex, "Rollback: could not drop database {Database}.", tenant.DatabaseName); }

        try
        {
            catalog.Tenants.Remove(tenant);
            await catalog.SaveChangesAsync();
        }
        catch (Exception ex) { logger.LogError(ex, "Rollback: could not remove tenant row {Tenant}.", tenant.Identifier); }
    }

    private async Task<Tenant> FindAsync(Guid id, bool track = false)
    {
        var query = track ? catalog.Tenants : catalog.Tenants.AsNoTracking();
        return await query.FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new BaseServiceException($"Tenant {id} not found.", ExceptionCodes.ItemNotFound);
    }

    private static void ThrowIfFailed(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new BaseServiceException(string.Join(" ", result.Errors.Select(e => e.Description)), ExceptionCodes.Validation);
    }

    private static TenantInfo ToInfo(Tenant t) => new(t.Id, t.Identifier, t.Name, t.DatabaseName, t.IsActive);

    private static TenantResponse ToResponse(Tenant t) => new()
    {
        Id = t.Id,
        Identifier = t.Identifier,
        Name = t.Name,
        DatabaseName = t.DatabaseName,
        IsActive = t.IsActive,
        GoogleWorkspaceDomain = t.GoogleWorkspaceDomain,
        MicrosoftEntraTenantId = t.MicrosoftEntraTenantId,
        CreatedAt = t.CreatedAt,
    };
}
