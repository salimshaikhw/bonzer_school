using Domain.Entities.Academics;
using Domain.Entities.Organization;
using Domain.Entities.Personnel;
using Infrastructure.Persistence.Repositories.Academics;
using Infrastructure.Persistence.Repositories.Organization;
using Infrastructure.Persistence.Repositories.Personnel;
using Infrastructure.Services.Academics;

namespace BonzerSchool.UnitTests.TestSupport;

/// <summary>Seeds the in-memory tenant database with the records most tests need.</summary>
public static class TestData
{
    public static Branch AddBranch(this TestEnvironment env, string name = "Main Campus")
    {
        var branch = new Branch { Id = Guid.NewGuid(), Name = name, GroupId = env.Tenant.Id };
        env.Db.Branches.Add(branch);
        env.Db.SaveChanges();
        return branch;
    }

    public static Class AddClass(this TestEnvironment env, Guid branchId, string name = "Class 5")
    {
        var cls = new Class { Id = Guid.NewGuid(), BranchId = branchId, Name = name };
        env.Db.Classes.Add(cls);
        env.Db.SaveChanges();
        return cls;
    }

    public static Division AddDivision(this TestEnvironment env, Class cls, string name = "A", int? seats = 40)
    {
        var division = new Division { Id = Guid.NewGuid(), ClassId = cls.Id, BranchId = cls.BranchId, Name = name, AvailableSeats = seats };
        env.Db.Divisions.Add(division);
        env.Db.SaveChanges();
        return division;
    }

    public static Subject AddSubject(this TestEnvironment env, string name = "Mathematics", string? code = "MATH")
    {
        var subject = new Subject { Id = Guid.NewGuid(), Name = name, Code = code };
        env.Db.Subjects.Add(subject);
        env.Db.SaveChanges();
        return subject;
    }

    public static Staff AddStaff(this TestEnvironment env, Guid branchId, string firstName = "Asha")
    {
        var staff = new Staff { Id = Guid.NewGuid(), BranchId = branchId, FirstName = firstName, LastName = "Patil" };
        env.Db.Staff.Add(staff);
        env.Db.SaveChanges();
        return staff;
    }

    /// <summary>The real reference checker of the Academics services, on this environment's database.</summary>
    public static AcademicStructure AcademicStructure(this TestEnvironment env) => new(
        new BranchRepository(env.Db, TestEnvironment.Logger<BranchRepository>()),
        new ClassRepository(env.Db, TestEnvironment.Logger<ClassRepository>()),
        new DivisionRepository(env.Db, TestEnvironment.Logger<DivisionRepository>()),
        new SubjectRepository(env.Db, TestEnvironment.Logger<SubjectRepository>()),
        new StaffRepository(env.Db, TestEnvironment.Logger<StaffRepository>()));
}

/// <summary>A clock fixed at a given instant, for date-dependent business rules.</summary>
public sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
