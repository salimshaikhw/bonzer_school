using Application.Abstractions;
using Application.Abstractions.Repositories.Academics;
using Application.Abstractions.Repositories.Attendances;
using Application.Abstractions.Repositories.Fees;
using Application.Abstractions.Services.Students;
using Application.Students;
using Application.ViewModels.ParentUser;
using Application.ViewModels.Student;
using BonzerSchool.UnitTests.TestSupport;
using Domain.Entities.Academics;
using Domain.Entities.Organization;
using Domain.Entities.Students;
using Infrastructure.Persistence.Repositories.Academics;
using Infrastructure.Persistence.Repositories.Organization;
using Infrastructure.Persistence.Repositories.Students;
using Infrastructure.Services.Common;
using Infrastructure.Services.Students;
using Microsoft.Extensions.Caching.Memory;

namespace BonzerSchool.UnitTests.Students;

public class StudentServiceTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 11, 3, 6, 0, 0, TimeSpan.Zero);
    private readonly TestEnvironment _env = new();
    private readonly StudentService _service;
    private readonly Branch _branch;
    private readonly Branch _otherBranch;
    private readonly Class _class5;
    private readonly Class _class6;
    private readonly Division _class5A;
    private readonly AcademicYear _year;

    public StudentServiceTests()
    {
        _branch = _env.AddBranch();
        _otherBranch = _env.AddBranch("City Campus");
        _class5 = _env.AddClass(_branch.Id, "Class 5");
        _class6 = _env.AddClass(_branch.Id, "Class 6");
        _class5A = _env.AddDivision(_class5, "A");
        _year = new AcademicYear { Id = Guid.NewGuid(), YearName = "2026-27" };
        _env.Db.AcademicYears.Add(_year);
        _env.Db.SaveChanges();

        var db = _env.Db;
        _service = new StudentService(new StudentServiceDependencies(
            new StudentRepository(db, TestEnvironment.Logger<StudentRepository>()),
            Substitute.For<IUnitOfWork>(),
            _env.Mapper,
            new CacheService(new MemoryCache(new MemoryCacheOptions()), _env.TenantContext),
            Substitute.For<ITeacherSubjectMapRepository>(),
            Substitute.For<IInvoiceRepository>(),
            Substitute.For<IAttendanceRepository>(),
            new ClassRepository(db, TestEnvironment.Logger<ClassRepository>()),
            new DivisionRepository(db, TestEnvironment.Logger<DivisionRepository>()),
            new BranchRepository(db, TestEnvironment.Logger<BranchRepository>()),
            new AcademicYearRepository(db, TestEnvironment.Logger<AcademicYearRepository>()),
            _env.AcademicStructure(),
            new FixedClock(Now)), TestEnvironment.Logger<StudentService>());
    }

    private StudentRequest Admission(string admissionNo = "ADM-1", string? aadhaar = null) => new()
    {
        BranchId = _branch.Id, ClassId = _class5.Id, DivisionId = _class5A.Id, AcademicYearId = _year.Id,
        FirstName = "Riya", LastName = "Shah", AdmissionNo = admissionNo, ChildAadhaar = aadhaar, RollNumber = "12",
    };

    private async Task<Student> Saved(Guid id)
    {
        _env.Db.ChangeTracker.Clear();
        return (await _env.Db.Students.FindAsync(id))!;
    }

    // ---- admission rules ----

    [Fact]
    public async Task A_new_student_is_active_with_normalised_identifiers()
    {
        var created = await _service.CreateAsync(Admission(" ADM-1 ", "4987 6543 2102"));

        var student = await Saved(created.Id);
        Assert.Equal(StudentStatuses.Active, student.StudentStatus);
        Assert.True(student.IsActive);
        Assert.Equal("ADM-1", student.AdmissionNo);
        Assert.Equal("498765432102", student.ChildAadhaar);
    }

    [Fact]
    public async Task Admission_numbers_are_unique_per_branch()
    {
        await _service.CreateAsync(Admission("ADM-1"));

        var duplicate = Admission("ADM-1");
        duplicate.RollNumber = "13";
        await ErrorAssert.ConflictAsync(() => _service.CreateAsync(duplicate));
    }

    [Fact]
    public async Task Aadhaar_numbers_are_unique_whatever_the_formatting()
    {
        await _service.CreateAsync(Admission("ADM-1", "498765432102"));

        var sibling = Admission("ADM-2", "4987-6543-2102");
        sibling.RollNumber = "13";
        await ErrorAssert.ConflictAsync(() => _service.CreateAsync(sibling));
    }

    [Fact]
    public async Task Roll_numbers_are_unique_within_a_division()
    {
        await _service.CreateAsync(Admission("ADM-1"));

        await ErrorAssert.ConflictAsync(() => _service.CreateAsync(Admission("ADM-2")));
    }

    [Fact]
    public async Task The_class_must_belong_to_the_branch_and_the_division_to_the_class()
    {
        var wrongBranch = Admission();
        wrongBranch.BranchId = _otherBranch.Id;
        await ErrorAssert.BadRequestAsync(() => _service.CreateAsync(wrongBranch));

        var wrongDivision = Admission();
        wrongDivision.ClassId = _class6.Id;
        await ErrorAssert.BadRequestAsync(() => _service.CreateAsync(wrongDivision));
    }

    [Fact]
    public async Task Editing_cannot_change_the_status_or_reset_it()
    {
        var created = await _service.CreateAsync(Admission());
        await _service.SuspendStudentAsync(created.Id);

        var edit = Admission();
        edit.StudentStatus = StudentStatuses.Active;
        await ErrorAssert.ConflictAsync(() => _service.UpdateAsync(created.Id, edit));

        // An edit form that leaves the status out keeps it.
        edit.StudentStatus = null;
        edit.FirstName = "Riya R.";
        await _service.UpdateAsync(created.Id, edit);
        var student = await Saved(created.Id);
        Assert.Equal(StudentStatuses.Suspended, student.StudentStatus);
        Assert.Equal("Riya R.", student.FirstName);
    }

    // ---- lifecycle through the service ----

    [Fact]
    public async Task A_dropped_student_cannot_be_promoted()
    {
        var created = await _service.CreateAsync(Admission());
        await _service.DropStudentAsync(created.Id);

        await ErrorAssert.ConflictAsync(() => _service.PromoteStudentAsync(created.Id, _class6.Id, _year.Id));
        var eligibility = await _service.ValidatePromotionEligibilityAsync(created.Id);
        Assert.False(eligibility.IsValid);
    }

    [Fact]
    public async Task Promotion_moves_to_the_new_class_and_clears_the_old_division()
    {
        var created = await _service.CreateAsync(Admission());

        await _service.PromoteStudentAsync(created.Id, _class6.Id, _year.Id);

        var student = await Saved(created.Id);
        Assert.Equal(_class6.Id, student.ClassId);
        Assert.Equal(_class6.Id, student.PromotedToClassId);
        Assert.Null(student.DivisionId);
        Assert.Null(student.RollNumber);
    }

    [Fact]
    public async Task Promotion_stays_within_the_branch()
    {
        var created = await _service.CreateAsync(Admission());
        var elsewhere = _env.AddClass(_otherBranch.Id, "Class 6");

        await ErrorAssert.BadRequestAsync(() => _service.PromoteStudentAsync(created.Id, elsewhere.Id, _year.Id));
        await ErrorAssert.BadRequestAsync(() => _service.PromoteStudentAsync(created.Id, _class6.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task A_branch_transfer_keeps_the_student_active_in_the_new_branch()
    {
        var created = await _service.CreateAsync(Admission());

        await _service.TransferStudentAsync(created.Id, _otherBranch.Id);

        var student = await Saved(created.Id);
        Assert.Equal(_otherBranch.Id, student.BranchId);
        Assert.Equal(StudentStatuses.Active, student.StudentStatus);
        Assert.Null(student.ClassId);
        Assert.Null(student.DivisionId);
    }

    [Fact]
    public async Task Transfer_to_an_unknown_or_the_same_branch_is_refused()
    {
        var created = await _service.CreateAsync(Admission());

        await ErrorAssert.BadRequestAsync(() => _service.TransferStudentAsync(created.Id, Guid.NewGuid()));
        await ErrorAssert.ConflictAsync(() => _service.TransferStudentAsync(created.Id, _branch.Id));
    }

    [Fact]
    public async Task Issuing_a_tc_deactivates_and_dates_the_leaving()
    {
        var created = await _service.CreateAsync(Admission());

        await _service.IssueTcAsync(created.Id);

        var student = await Saved(created.Id);
        Assert.Equal(StudentStatuses.Transferred, student.StudentStatus);
        Assert.False(student.IsActive);
        Assert.True(student.IsTcIssued);
        Assert.Equal(new DateOnly(2026, 11, 3), student.LeavingDate);
        await ErrorAssert.ConflictAsync(() => _service.IssueTcAsync(created.Id));
    }

    [Fact]
    public async Task The_generic_status_endpoint_follows_the_same_rules()
    {
        var created = await _service.CreateAsync(Admission());

        await ErrorAssert.BadRequestAsync(() => _service.UpdateStudentStatusAsync(created.Id, "GRADUATED"));
        await _service.UpdateStudentStatusAsync(created.Id, "passed_out");
        await ErrorAssert.ConflictAsync(() => _service.UpdateStudentStatusAsync(created.Id, "SUSPENDED"));
    }

    [Fact]
    public async Task Leaving_date_cannot_be_before_admission()
    {
        var request = Admission();
        request.AdmissionDate = new DateOnly(2025, 6, 1);
        var created = await _service.CreateAsync(request);

        await ErrorAssert.BadRequestAsync(() => _service.UpdateLeavingDateAsync(created.Id, new DateOnly(2025, 5, 1)));
    }

    [Fact]
    public async Task Existence_checks_see_new_students_immediately()
    {
        Assert.False(await _service.CheckAdmissionNumberExistsAsync(_branch.Id, "ADM-9"));

        var request = Admission("ADM-9");
        await _service.CreateAsync(request);

        Assert.True(await _service.CheckAdmissionNumberExistsAsync(_branch.Id, " ADM-9 "));
    }

    public void Dispose() => _env.Dispose();
}

public class ParentLinkServiceTests : IDisposable
{
    private readonly TestEnvironment _env = new();
    private readonly IParentAccounts _accounts = Substitute.For<IParentAccounts>();
    private readonly ParentUserService _service;
    private readonly Guid _studentId;

    public ParentLinkServiceTests()
    {
        _studentId = Guid.NewGuid();
        _env.Db.Students.Add(new Student { Id = _studentId, FirstName = "Riya" });
        _env.Db.SaveChanges();
        _accounts.IsParentLoginAsync("parent@bonzer.local").Returns(true);
        _service = new ParentUserService(TestEnvironment.Logger<ParentUserService>(),
            new ParentUserRepository(_env.Db, TestEnvironment.Logger<ParentUserRepository>()), _env.Mapper,
            new StudentRepository(_env.Db, TestEnvironment.Logger<StudentRepository>()), _accounts);
    }

    [Fact]
    public async Task A_child_is_linked_to_an_existing_parent_login_once_and_no_password_is_stored()
    {
        var link = await _service.CreateAsync(new ParentUserRequest { StudentId = _studentId, Username = " Parent@Bonzer.local ", PasswordHash = "plain-text!" });

        var saved = (await _env.Db.ParentUsers.FindAsync(link.Id))!;
        Assert.Equal("parent@bonzer.local", saved.Username);
        Assert.Null(saved.PasswordHash);
        await ErrorAssert.ConflictAsync(() => _service.CreateAsync(new ParentUserRequest { StudentId = _studentId, Username = "parent@bonzer.local" }));
    }

    [Fact]
    public async Task Unknown_logins_and_students_cannot_be_linked()
    {
        await ErrorAssert.BadRequestAsync(() => _service.CreateAsync(new ParentUserRequest { StudentId = _studentId, Username = "stranger@mail.com" }));
        await ErrorAssert.BadRequestAsync(() => _service.CreateAsync(new ParentUserRequest { StudentId = Guid.NewGuid(), Username = "parent@bonzer.local" }));
    }

    public void Dispose() => _env.Dispose();
}
