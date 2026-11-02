using Application.Common.Constants;
using Domain.Enums;
using Infrastructure.Persistence.Catalog;
using Infrastructure.Persistence.Postgres;
using Infrastructure.Security.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Seed;

/// <summary>
/// Development demo data for the demo tenant: one login per role and a small school to log in to.
/// Logins are "{role}@bonzer.local" (e.g. principal@bonzer.local) with the DemoUsers:Password.
/// Staff logins get a staff record with the same email (that is how the app maps a login to a
/// staff member); the parent login is linked to two students through parent_users.username.
/// The school is only created when no branch exists yet.
/// </summary>
internal static class DemoDataSeeder
{
    private const string Domain = "bonzer.local";
    private const string BranchName = "Bonzer International School - Main Campus";

    private static readonly (string Role, string FirstName, string LastName, string Designation, string Department)[] StaffAccounts =
    {
        (Roles.BranchAdmin,   "Branch",  "Admin",     "Office Superintendent", "Administration"),
        (Roles.Principal,     "Sunita",  "Deshmukh",  "Principal",             "Administration"),
        (Roles.VicePrincipal, "Rajesh",  "Kulkarni",  "Vice Principal",        "Administration"),
        (Roles.Accountant,    "Meena",   "Joshi",     "Accountant",            "Accounts"),
        (Roles.Hod,           "Anil",    "Patil",     "HOD - Science",         "Teaching"),
        (Roles.Teacher,       "Kavita",  "Shah",      "Teacher",               "Teaching"),
        (Roles.ClassTeacher,  "Priya",   "Nair",      "Class Teacher (1-A)",   "Teaching"),
        (Roles.SportsTeacher, "Vikram",  "Singh",     "Sports Teacher",        "Sports"),
        (Roles.Librarian,     "Asha",    "Kale",      "Librarian",             "Library"),
    };

    private static readonly (string Name, string Code)[] Subjects =
    {
        ("English", "ENG"), ("Hindi", "HIN"), ("Marathi", "MAR"), ("Mathematics", "MAT"), ("Science", "SCI"),
        ("Social Studies", "SST"), ("Computer Science", "CS"), ("Physical Education", "PE"), ("Art", "ART"),
    };

    private static readonly (string First, string Last, string Gender)[] Students =
    {
        ("Aarav", "Sharma", "Male"), ("Ananya", "Sharma", "Female"), ("Vivaan", "Patel", "Male"),
        ("Diya", "Mehta", "Female"), ("Arjun", "Rao", "Male"), ("Saanvi", "Iyer", "Female"),
        ("Reyansh", "Gupta", "Male"), ("Myra", "Kapoor", "Female"), ("Kabir", "Verma", "Male"),
        ("Ira", "Menon", "Female"),
    };

    /// <param name="db">The demo tenant's database.</param>
    /// <param name="catalog">Catalog, for the logins and the shared institution type / medium lookups.</param>
    public static async Task SeedAsync(
        BonzerSchoolDbContext db, CatalogDbContext catalog, UserManager<ApplicationUser> userManager, ILogger logger,
        string password, Guid tenantId)
    {
        // Logins for every non-admin role (the Group Admin was created with the tenant).
        foreach (var account in StaffAccounts)
            await DatabaseInitializer.EnsureUserAsync(userManager, logger, Email(account.Role), password, account.FirstName, account.LastName, account.Role, tenantId);
        await DatabaseInitializer.EnsureUserAsync(userManager, logger, Email(Roles.Parent), password, "Rohit", "Sharma", Roles.Parent, tenantId);
        await DatabaseInitializer.EnsureUserAsync(userManager, logger, Email(Roles.Student), password, "Aarav", "Sharma", Roles.Student, tenantId);

        if (await db.Branches.AnyAsync())
        {
            logger.LogInformation("Demo school skipped: branches already exist.");
            return;
        }

        // Ids are set here (not by the database) so related rows can reference them before SaveChanges.
        // The school_groups row was created with the tenant and has the tenant's id.
        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            GroupId = tenantId,
            InstitutionTypeId = await catalog.InstitutionTypes.Where(x => x.Name == "School").Select(x => (Guid?)x.Id).FirstOrDefaultAsync(),
            MediumId = await catalog.Mediums.Where(x => x.Name == "English").Select(x => (Guid?)x.Id).FirstOrDefaultAsync(),
            SchoolUid = "BIS-MAIN", Name = BranchName,
            Address = "12 MG Road", City = "Pune", State = "Maharashtra",
            Phone = "020-24000001", Email = $"main@{Domain}",
            AttendanceSundayOff = true, AttendanceSaturdayOff = false,
            AttendanceSaturdayPattern = AttendanceSaturdayPattern.SecondFourth,
        };
        db.Branches.Add(branch);

        // Indian academic year: April to March.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startYear = today.Month >= 4 ? today.Year : today.Year - 1;
        var academicYear = new AcademicYear
        {
            Id = Guid.NewGuid(),
            YearName = $"{startYear}-{(startYear + 1) % 100:00}",
            StartDate = new DateOnly(startYear, 4, 1),
            EndDate = new DateOnly(startYear + 1, 3, 31),
            IsActive = true,
        };
        db.AcademicYears.Add(academicYear);

        foreach (var (name, code) in Subjects)
            if (!await db.Subjects.AnyAsync(s => s.Name == name))
                db.Subjects.Add(new Subject { Name = name, Code = code });

        // Classes 1-10, divisions A and B.
        Class? class1 = null;
        Division? class1A = null;
        for (var n = 1; n <= 10; n++)
        {
            var cls = new Class { Id = Guid.NewGuid(), BranchId = branch.Id, Name = $"Class {n}" };
            db.Classes.Add(cls);
            foreach (var div in new[] { "A", "B" })
            {
                var division = new Division { Id = Guid.NewGuid(), ClassId = cls.Id, BranchId = branch.Id, Name = div, Code = $"{n}{div}", AvailableSeats = 40 };
                db.Divisions.Add(division);
                if (n == 1 && div == "A") { class1 = cls; class1A = division; }
            }
        }

        // TC no-dues departments for this branch.
        foreach (var dept in new[] { "Library", "Accounts", "Sports", "Laboratory" })
            db.ClearanceDepartments.Add(new ClearanceDepartment { DepartmentName = dept, BranchId = branch.Id, IsMandatory = true });

        // Staff records for the staff logins, linked to their designation in the roles table.
        var departments = await db.Departments.ToDictionaryAsync(d => d.Name!, d => d.Id);
        var schoolRoles = await db.SchoolRoles.ToDictionaryAsync(r => r.Name!, r => r.Id);
        Staff? classTeacher = null;
        var staffNo = 1;
        foreach (var account in StaffAccounts)
        {
            var staff = new Staff
            {
                Id = Guid.NewGuid(),
                BranchId = branch.Id,
                DeptId = departments.TryGetValue(account.Department, out var deptId) ? deptId : null,
                StaffCode = $"BIS-{staffNo++:000}",
                Designation = account.Designation,
                FirstName = account.FirstName, LastName = account.LastName,
                Email = Email(account.Role),
                Phone = $"98{staffNo:00000000}",
                JoiningDate = new DateOnly(startYear, 4, 1),
                IsActive = true,
            };
            db.Staff.Add(staff);
            db.StaffRoles.Add(new StaffRole { StaffId = staff.Id, RoleId = schoolRoles[account.Role] });
            if (account.Role == Roles.ClassTeacher) classTeacher = staff;
        }

        db.ClassTeacherMaps.Add(new ClassTeacherMap
        {
            ClassId = class1!.Id, DivisionId = class1A!.Id, StaffId = classTeacher!.Id, AcademicYear = academicYear.YearName,
        });

        // Students of Class 1-A. The parent login is the parent of the first two (siblings).
        var roll = 1;
        var studentIds = new List<Guid>();
        foreach (var (first, last, gender) in Students)
        {
            var student = new Student
            {
                Id = Guid.NewGuid(),
                BranchId = branch.Id, AcademicYearId = academicYear.Id,
                ClassId = class1.Id, DivisionId = class1A.Id,
                AdmissionNo = $"ADM-{startYear}-{roll:000}", AdmissionDate = new DateOnly(startYear, 4, 1),
                RollNumber = roll.ToString(),
                FirstName = first, LastName = last, Gender = gender,
                Dob = new DateOnly(startYear - 6, 1 + roll % 12, 1 + roll),
                FatherName = $"Mr. {last}", MotherName = $"Mrs. {last}",
                FatherPhone = $"97{roll:00000000}",
                City = "Pune", State = "Maharashtra", Nationality = "Indian",
                Email = roll == 1 ? Email(Roles.Student) : null,
                IsActive = true,
            };
            db.Students.Add(student);
            studentIds.Add(student.Id);
            roll++;
        }
        foreach (var studentId in studentIds.Take(2))
            db.ParentUsers.Add(new ParentUser { StudentId = studentId, Username = Email(Roles.Parent), Phone = "9700000001" });

        await db.SaveChangesAsync();
        logger.LogInformation("Demo school seeded: {Branch}, {Year}, {Classes} classes, {Staff} staff, {Students} students.",
            BranchName, academicYear.YearName, 10, StaffAccounts.Length, Students.Length);
    }

    private static string Email(string role) => $"{role.ToLowerInvariant()}@{Domain}";
}
