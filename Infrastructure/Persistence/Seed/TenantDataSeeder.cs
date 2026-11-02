using Application.Abstractions.Tenancy;
using Application.Common.Constants;
using Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds one tenant database: its school_groups row (same Id as the catalog tenant), the legacy roles
/// table and the default reference data each school starts with and may then edit (exam types, grades,
/// fee heads, departments...). Idempotent: runs on provisioning and after every startup migration.
/// </summary>
internal static class TenantDataSeeder
{
    public static async Task SeedAsync(BonzerSchoolDbContext db, TenantInfo tenant, SchoolGroup? groupDetails = null)
    {
        if (!await db.SchoolGroups.AnyAsync(g => g.Id == tenant.Id))
        {
            var group = groupDetails ?? new SchoolGroup();
            group.Id = tenant.Id;
            group.Name ??= tenant.Name;
            db.SchoolGroups.Add(group);
            await db.SaveChangesAsync();
        }

        // Legacy "roles" table, used by staff_roles to record a staff member's designation.
        var existingRoles = await db.SchoolRoles.Select(r => r.Name).ToListAsync();
        foreach (var role in Roles.All.Where(r => !existingRoles.Contains(r)))
            db.SchoolRoles.Add(new Role { Name = role, Description = Roles.Descriptions[role] });
        await db.SaveChangesAsync();

        await DatabaseInitializer.AddMissingAsync(db, x => x.Name,
            new ExamType { Name = "Unit Test 1", Description = "First periodic test" },
            new ExamType { Name = "Unit Test 2", Description = "Second periodic test" },
            new ExamType { Name = "First Term", Description = "Mid-year (half-yearly) examination" },
            new ExamType { Name = "Second Term", Description = "Annual (final) examination" });

        await DatabaseInitializer.AddMissingAsync(db, x => x.Name,
            new ExamComponentType { Name = "Theory", Code = "TH" },
            new ExamComponentType { Name = "Practical", Code = "PR" },
            new ExamComponentType { Name = "Oral", Code = "OR" },
            new ExamComponentType { Name = "Internal Assessment", Code = "IA" },
            new ExamComponentType { Name = "Project", Code = "PJ" });

        // CBSE-style grade bands.
        await DatabaseInitializer.AddMissingAsync(db, x => x.Grade,
            new GradeMaster { Grade = "A1", MinPercent = 91, MaxPercent = 100 },
            new GradeMaster { Grade = "A2", MinPercent = 81, MaxPercent = 90.99m },
            new GradeMaster { Grade = "B1", MinPercent = 71, MaxPercent = 80.99m },
            new GradeMaster { Grade = "B2", MinPercent = 61, MaxPercent = 70.99m },
            new GradeMaster { Grade = "C1", MinPercent = 51, MaxPercent = 60.99m },
            new GradeMaster { Grade = "C2", MinPercent = 41, MaxPercent = 50.99m },
            new GradeMaster { Grade = "D", MinPercent = 33, MaxPercent = 40.99m },
            new GradeMaster { Grade = "E", MinPercent = 0, MaxPercent = 32.99m });

        await DatabaseInitializer.AddMissingAsync(db, x => x.Name,
            new FeeHead { Name = "Admission Fee", AccountCode = "FEE-ADM" },
            new FeeHead { Name = "Tuition Fee", AccountCode = "FEE-TUI" },
            new FeeHead { Name = "Exam Fee", AccountCode = "FEE-EXM" },
            new FeeHead { Name = "Library Fee", AccountCode = "FEE-LIB" },
            new FeeHead { Name = "Sports Fee", AccountCode = "FEE-SPT" },
            new FeeHead { Name = "Computer Fee", AccountCode = "FEE-CMP" },
            new FeeHead { Name = "Transport Fee", AccountCode = "FEE-TRN" },
            new FeeHead { Name = "Development Fee", AccountCode = "FEE-DEV" });

        // Staff departments (table "department"; names are at most 20 characters).
        await DatabaseInitializer.AddMissingAsync(db, x => x.Name,
            new Department { Name = "Administration" },
            new Department { Name = "Accounts" },
            new Department { Name = "Teaching" },
            new Department { Name = "Library" },
            new Department { Name = "Sports" });
    }
}
