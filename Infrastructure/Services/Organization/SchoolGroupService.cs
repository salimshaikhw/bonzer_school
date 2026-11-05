using System.Text.RegularExpressions;
using Application.Abstractions.Tenancy;
using Application.Common;
using Application.Common.Constants;
using Application.Common.Extensions;
using Application.Exceptions;
using Application.ViewModels.SchoolGroup;
using Infrastructure.Persistence.Catalog;
using Infrastructure.Persistence.Seed;
using Infrastructure.Security.Entities;
using Infrastructure.Tenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Organization;

/// <summary>
/// School groups (trusts) = tenants, stored in the catalog table "organizations". Hierarchy:
/// school group (its own database) -> branches -> classes, staff, students...
/// Creating a school group is all-or-nothing: if the database, seed or admin login fails, the new
/// database, login and catalog row are removed again.
/// A tenant user can only read and edit their own group; the Super Admin manages all of them.
/// </summary>
public sealed partial class SchoolGroupService(
    CatalogDbContext catalog,
    UserManager<ApplicationUser> userManager,
    TenantConnectionStrings connectionStrings,
    TenantDbContextFactory tenantDbFactory,
    ITenantStore tenantStore,
    ITenantContext tenantContext,
    ILogger<SchoolGroupService> logger) : ISchoolGroupService
{
    // 3-40 characters: lower-case letters, digits and inner hyphens; starts with a letter.
    [GeneratedRegex("^[a-z][a-z0-9-]{1,38}[a-z0-9]$")]
    private static partial Regex IdentifierPattern();

    public async Task<IEnumerable<SchoolGroupDto>> GetAllAsync()
    {
        var query = catalog.SchoolGroups.AsNoTracking();
        if (tenantContext.TenantId is { } own)
            query = query.Where(g => g.Id == own);
        return (await query.OrderBy(g => g.Name).ToListAsync()).Select(ToDto).ToList();
    }

    public async Task<SchoolGroupDto> GetByIdAsync(Guid id)
    {
        EnsureVisible(id);
        return ToDto(await FindAsync(id));
    }

    public async Task<SchoolGroupDto> CreateAsync(CreateSchoolGroupInput input)
    {
        var identifier = input.Identifier?.Trim().ToLowerInvariant() ?? string.Empty;
        await ValidateAsync(input, identifier);

        var group = new SchoolGroup
        {
            Id = Guid.NewGuid(),
            Identifier = identifier,
            Name = input.Name.Trim(),
            Code = input.Code,
            Address = input.Address,
            Phone = input.Phone,
            Email = input.Email,
            DatabaseName = connectionStrings.DatabaseNameFor(identifier),
            IsActive = false, // switched on once everything below has succeeded
        };
        var info = ToInfo(group);

        await using var tenantDb = tenantDbFactory.Create(info);
        if (await tenantDb.GetService<IRelationalDatabaseCreator>().ExistsAsync())
            throw new BaseServiceException(
                $"Database '{group.DatabaseName}' already exists on the server. Choose another identifier.", ExceptionCodes.Validation);

        catalog.SchoolGroups.Add(group);
        await catalog.SaveChangesAsync();

        ApplicationUser? admin = null;
        try
        {
            await tenantDb.Database.MigrateAsync();
            await TenantDataSeeder.SeedAsync(tenantDb);

            var newAdmin = new ApplicationUser
            {
                TenantId = group.Id,
                UserName = input.AdminEmail.Trim(),
                Email = input.AdminEmail.Trim(),
                EmailConfirmed = true,
                FirstName = input.AdminFirstName,
                LastName = input.AdminLastName,
                DisplayName = $"{input.AdminFirstName} {input.AdminLastName}",
                CreatedOn = DateTime.UtcNow,
                CreatedBy = "platform",
            };
            ThrowIfFailed(await userManager.CreateAsync(newAdmin, input.AdminPassword));
            admin = newAdmin;
            ThrowIfFailed(await userManager.AddToRoleAsync(admin, Roles.GroupAdmin));

            group.IsActive = true;
            await catalog.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Provisioning school group {Group} failed; rolling back.", identifier);
            await RollBackProvisioningAsync(group, admin, tenantDb);
            throw;
        }

        logger.LogInformation("School group {Group} provisioned in database {Database}.", identifier, group.DatabaseName);
        return ToDto(group);
    }

    public async Task<SchoolGroupDto> UpdateAsync(Guid id, UpdateSchoolGroupInput input)
    {
        EnsureVisible(id);
        var group = await FindAsync(id, track: true);
        group.Name = input.Name.Trim();
        group.Code = input.Code;
        group.Address = input.Address;
        group.Phone = input.Phone;
        group.Email = input.Email;
        await catalog.SaveChangesAsync();
        tenantStore.Invalidate(group.Id, group.Identifier);
        return ToDto(group);
    }

    public async Task<SchoolGroupDto> SetActiveAsync(Guid id, bool isActive)
    {
        var group = await FindAsync(id, track: true);
        group.IsActive = isActive;
        await catalog.SaveChangesAsync();
        tenantStore.Invalidate(group.Id, group.Identifier);
        logger.LogInformation("School group {Group} {State}.", group.Identifier, isActive ? "activated" : "deactivated");
        return ToDto(group);
    }

    public async Task<SchoolGroupDto> SetExternalLoginAsync(Guid id, SetExternalLoginRequest request)
    {
        var group = await FindAsync(id, track: true);
        var domain = request.GoogleWorkspaceDomain?.Trim().ToLowerInvariant();
        group.GoogleWorkspaceDomain = string.IsNullOrEmpty(domain) ? null : domain;
        group.MicrosoftEntraTenantId = request.MicrosoftEntraTenantId == Guid.Empty ? null : request.MicrosoftEntraTenantId;
        await catalog.SaveChangesAsync();
        logger.LogInformation("School group {Group} external login: Google domain {Domain}, Entra tenant {Entra}.",
            group.Identifier, group.GoogleWorkspaceDomain ?? "-", group.MicrosoftEntraTenantId?.ToString() ?? "-");
        return ToDto(group);
    }

    public async Task<IReadOnlyList<string>> MigrateAllAsync(CancellationToken cancellationToken = default)
    {
        var groups = await catalog.SchoolGroups.AsNoTracking().Where(g => g.IsActive).ToListAsync(cancellationToken);
        var migrated = new List<string>();
        foreach (var group in groups)
        {
            try
            {
                await using var db = tenantDbFactory.Create(ToInfo(group));
                await db.Database.MigrateAsync(cancellationToken);
                await TenantDataSeeder.SeedAsync(db);
                migrated.Add(group.Identifier);
            }
            catch (Exception ex)
            {
                // One broken database must not stop the others (or the API) from starting.
                logger.LogError(ex, "Migrating school group {Group} ({Database}) failed.", group.Identifier, group.DatabaseName);
            }
        }
        logger.LogInformation("School group databases up to date: {Migrated}/{Total}.", migrated.Count, groups.Count);
        return migrated;
    }

    /// <summary>A tenant user can only see their own group; anything else is reported as not found.</summary>
    private void EnsureVisible(Guid id)
    {
        if (tenantContext.TenantId is { } own && id != own)
            throw new BaseServiceException($"School group {id} not found.", ExceptionCodes.ItemNotFound);
    }

    private async Task ValidateAsync(CreateSchoolGroupInput input, string identifier)
    {
        if (!IdentifierPattern().IsMatch(identifier))
            throw new BaseServiceException(
                "Identifier must be 3-40 characters: lower-case letters, digits and hyphens, starting with a letter.", ExceptionCodes.Validation);
        if (string.IsNullOrWhiteSpace(input.Name))
            throw new BaseServiceException("Name is required.", ExceptionCodes.Validation);
        if (string.IsNullOrWhiteSpace(input.AdminEmail) || string.IsNullOrEmpty(input.AdminPassword))
            throw new BaseServiceException("AdminEmail and AdminPassword are required.", ExceptionCodes.Validation);

        if (await catalog.SchoolGroups.AnyAsync(g => g.Identifier == identifier))
            throw new BaseServiceException($"School group '{identifier}' already exists.", ExceptionCodes.Validation);
        if (await userManager.FindByEmailAsync(input.AdminEmail.Trim()) != null)
            throw new BaseServiceException($"Email {input.AdminEmail} is already registered.", ExceptionCodes.Validation);

        // Check the password before any database is created.
        foreach (var validator in userManager.PasswordValidators)
            ThrowIfFailed(await validator.ValidateAsync(userManager, new ApplicationUser(), input.AdminPassword));
    }

    private async Task RollBackProvisioningAsync(SchoolGroup group, ApplicationUser? admin, DbContext tenantDb)
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
        catch (Exception ex) { logger.LogError(ex, "Rollback: could not drop database {Database}.", group.DatabaseName); }

        try
        {
            catalog.SchoolGroups.Remove(group);
            await catalog.SaveChangesAsync();
        }
        catch (Exception ex) { logger.LogError(ex, "Rollback: could not remove school group row {Group}.", group.Identifier); }
    }

    private async Task<SchoolGroup> FindAsync(Guid id, bool track = false)
    {
        var query = track ? catalog.SchoolGroups : catalog.SchoolGroups.AsNoTracking();
        return await query.FirstOrDefaultAsync(g => g.Id == id)
            ?? throw new BaseServiceException($"School group {id} not found.", ExceptionCodes.ItemNotFound);
    }

    private static void ThrowIfFailed(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new BaseServiceException(string.Join(" ", result.Errors.Select(e => e.Description)), ExceptionCodes.Validation);
    }

    private static TenantInfo ToInfo(SchoolGroup g) => new(g.Id, g.Identifier, g.Name, g.DatabaseName, g.IsActive);

    private static SchoolGroupDto ToDto(SchoolGroup g) => new SchoolGroupDto
    {
        Identifier = g.Identifier,
        Name = g.Name,
        Code = g.Code,
        Address = g.Address,
        Phone = g.Phone,
        Email = g.Email,
        DatabaseName = g.DatabaseName,
        IsActive = g.IsActive,
        GoogleWorkspaceDomain = g.GoogleWorkspaceDomain,
        MicrosoftEntraTenantId = g.MicrosoftEntraTenantId,
    }.WithAuditFrom(g);
}
