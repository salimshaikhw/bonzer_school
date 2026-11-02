using Application.Validation.Personnel;
using Application.ViewModels.Department;
using Application.ViewModels.Staff;
using Application.ViewModels.StaffRole;
using BonzerSchool.UnitTests.TestSupport;
using Domain.Entities.Academics;
using Domain.Entities.Auth;
using Domain.Entities.Personnel;
using Infrastructure.Persistence.Repositories.Academics;
using Infrastructure.Persistence.Repositories.Auth;
using Infrastructure.Persistence.Repositories.Personnel;
using Infrastructure.Services.Personnel;

namespace BonzerSchool.UnitTests.Personnel;

public class StaffServiceTests : IDisposable
{
    private readonly TestEnvironment _env = new();
    private readonly StaffService _service;
    private readonly Guid _branchId;

    public StaffServiceTests()
    {
        _branchId = _env.AddBranch().Id;
        _service = new StaffService(TestEnvironment.Logger<StaffService>(), new StaffRepository(_env.Db, TestEnvironment.Logger<StaffRepository>()),
            _env.Mapper, _env.AcademicStructure(), new DepartmentRepository(_env.Db, TestEnvironment.Logger<DepartmentRepository>()),
            new ClassTeacherMapRepository(_env.Db, TestEnvironment.Logger<ClassTeacherMapRepository>()),
            new TeacherSubjectMapRepository(_env.Db, TestEnvironment.Logger<TeacherSubjectMapRepository>()),
            new TimetableRepository(_env.Db, TestEnvironment.Logger<TimetableRepository>()));
    }

    private StaffRequest Staff(string code = "t-001", string? email = "asha@school.edu") =>
        new() { BranchId = _branchId, FirstName = " Asha ", LastName = "Patil", StaffCode = code, Email = email };

    [Fact]
    public async Task New_staff_are_active_with_normalised_code_and_email()
    {
        var created = await _service.CreateAsync(Staff(email: " Asha@School.EDU "));

        var saved = (await _env.Db.Staff.FindAsync(created.Id))!;
        Assert.True(saved.IsActive);
        Assert.Equal("T-001", saved.StaffCode);
        Assert.Equal("asha@school.edu", saved.Email);
        Assert.Equal("Asha", saved.FirstName);
    }

    [Fact]
    public async Task Staff_codes_are_unique_per_branch()
    {
        var otherBranch = _env.AddBranch("City Campus").Id;
        await _service.CreateAsync(Staff("T-001", "a@school.edu"));

        await ErrorAssert.ConflictAsync(() => _service.CreateAsync(Staff("t-001", "b@school.edu")));
        var elsewhere = Staff("T-001", "c@school.edu");
        elsewhere.BranchId = otherBranch;
        await _service.CreateAsync(elsewhere);
    }

    [Fact]
    public async Task Emails_are_unique()
    {
        await _service.CreateAsync(Staff("T-001", "a@school.edu"));

        await ErrorAssert.ConflictAsync(() => _service.CreateAsync(Staff("T-002", "A@school.edu")));
    }

    [Fact]
    public async Task Unknown_branch_or_department_is_rejected()
    {
        var request = Staff();
        request.DeptId = Guid.NewGuid();
        await ErrorAssert.BadRequestAsync(() => _service.CreateAsync(request));

        request = Staff();
        request.BranchId = Guid.NewGuid();
        await ErrorAssert.BadRequestAsync(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task Assigned_staff_cannot_be_deleted_but_unassigned_staff_can()
    {
        var assigned = await _service.CreateAsync(Staff("T-001", "a@school.edu"));
        var free = await _service.CreateAsync(Staff("T-002", "b@school.edu"));
        _env.Db.Timetables.Add(new Timetable { Id = Guid.NewGuid(), TeacherId = assigned.Id, DayOfWeek = 1, PeriodNo = 1 });
        await _env.Db.SaveChangesAsync();

        await ErrorAssert.ConflictAsync(() => _service.DeleteAsync(assigned.Id));
        Assert.True(await _service.DeleteAsync(free.Id));
    }

    public void Dispose() => _env.Dispose();
}

public class DepartmentAndStaffRoleServiceTests : IDisposable
{
    private readonly TestEnvironment _env = new();
    private readonly DepartmentService _departments;
    private readonly StaffRoleService _staffRoles;

    public DepartmentAndStaffRoleServiceTests()
    {
        var staffRepository = new StaffRepository(_env.Db, TestEnvironment.Logger<StaffRepository>());
        _departments = new DepartmentService(TestEnvironment.Logger<DepartmentService>(),
            new DepartmentRepository(_env.Db, TestEnvironment.Logger<DepartmentRepository>()), _env.Mapper, _env.Cache, staffRepository);
        _staffRoles = new StaffRoleService(TestEnvironment.Logger<StaffRoleService>(),
            new StaffRoleRepository(_env.Db, TestEnvironment.Logger<StaffRole>()), _env.Mapper, _env.AcademicStructure(),
            new RoleRepository(_env.Db, TestEnvironment.Logger<RoleRepository>()));
    }

    [Fact]
    public async Task Department_names_are_unique_and_departments_with_staff_stay()
    {
        var science = await _departments.CreateAsync(new DepartmentRequest { Name = "Science" });
        await ErrorAssert.ConflictAsync(() => _departments.CreateAsync(new DepartmentRequest { Name = "science " }));

        var staff = _env.AddStaff(_env.AddBranch().Id);
        staff.DeptId = science.Id;
        await _env.Db.SaveChangesAsync();
        await ErrorAssert.ConflictAsync(() => _departments.DeleteAsync(science.Id));
    }

    [Fact]
    public async Task A_role_is_held_once_and_must_exist()
    {
        var staff = _env.AddStaff(_env.AddBranch().Id);
        var role = new Role { Id = Guid.NewGuid(), Name = "Teacher" };
        _env.Db.SchoolRoles.Add(role);
        await _env.Db.SaveChangesAsync();

        await _staffRoles.CreateAsync(new StaffRoleRequest { StaffId = staff.Id, RoleId = role.Id });
        await ErrorAssert.ConflictAsync(() => _staffRoles.CreateAsync(new StaffRoleRequest { StaffId = staff.Id, RoleId = role.Id }));
        await ErrorAssert.BadRequestAsync(() => _staffRoles.CreateAsync(new StaffRoleRequest { StaffId = staff.Id, RoleId = Guid.NewGuid() }));
    }

    public void Dispose() => _env.Dispose();
}

public class StaffRequestValidatorTests
{
    private readonly StaffRequestValidator _validator = new();

    private static StaffRequest Valid() => new()
    {
        BranchId = Guid.NewGuid(), FirstName = "Asha", Dob = new DateOnly(1990, 5, 1), JoiningDate = new DateOnly(2020, 6, 1), Salary = 35000,
    };

    [Fact]
    public void A_normal_staff_record_is_valid()
    {
        var result = _validator.Validate(Valid());
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void Staff_must_be_adults_when_they_join()
    {
        var request = Valid();
        request.JoiningDate = request.Dob!.Value.AddYears(17);

        Assert.Contains(_validator.Validate(request).Errors, e => e.ErrorMessage.Contains("at least 18"));
    }

    [Theory]
    [InlineData(-1, null)]
    [InlineData(null, 61)]
    public void Salary_and_experience_must_be_sensible(int? salary, int? experience)
    {
        var request = Valid();
        request.Salary = salary;
        request.YearsOfExperience = experience;

        Assert.False(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Birth_date_must_be_in_the_past_and_joining_not_far_ahead()
    {
        var request = Valid();
        request.Dob = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        request.JoiningDate = null;
        Assert.False(_validator.Validate(request).IsValid);

        request = Valid();
        request.JoiningDate = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(2);
        Assert.False(_validator.Validate(request).IsValid);
    }
}
