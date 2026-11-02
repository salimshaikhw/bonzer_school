using Application.Abstractions;
using Application.Abstractions.Repositories.Academics;
using Application.Abstractions.Repositories.Attendances;
using Application.Abstractions.Repositories.Exams;
using Application.Abstractions.Repositories.Personnel;
using Application.Attendances;
using Application.Students;
using Application.ViewModels.Attendance;
using BonzerSchool.UnitTests.TestSupport;
using Domain.Entities.Academics;
using Domain.Entities.Attendances;
using Domain.Entities.Organization;
using Domain.Entities.Students;
using Infrastructure.Persistence.Repositories.Academics;
using Infrastructure.Persistence.Repositories.Attendances;
using Infrastructure.Persistence.Repositories.Organization;
using Infrastructure.Persistence.Repositories.Students;
using Infrastructure.Services.Attendances;

namespace BonzerSchool.UnitTests.Attendances;

public class AttendanceStatusTests
{
    [Theory]
    [InlineData("P", AttendanceStatuses.Present)]
    [InlineData(" present ", AttendanceStatuses.Present)]
    [InlineData("1", AttendanceStatuses.Present)]
    [InlineData("ab", AttendanceStatuses.Absent)]
    [InlineData("half-day", AttendanceStatuses.HalfDay)]
    [InlineData("LV", AttendanceStatuses.Leave)]
    [InlineData("late", AttendanceStatuses.Late)]
    public void Common_spellings_map_to_the_canonical_status(string input, string expected) =>
        Assert.Equal(expected, AttendanceStatuses.Normalize(input));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("excused")]
    public void Unknown_statuses_are_not_guessed(string? input) => Assert.Null(AttendanceStatuses.Normalize(input));
}

public class AttendanceServiceRuleTests : IDisposable
{
    private static readonly DateOnly Today = new(2026, 11, 3);
    private readonly TestEnvironment _env = new();
    private readonly AttendanceService _service;
    private readonly Class _class;
    private readonly Division _divisionA;
    private readonly Division _divisionB;
    private readonly Student _student;
    private readonly AcademicYear _year;

    public AttendanceServiceRuleTests()
    {
        var branch = _env.AddBranch();
        _class = _env.AddClass(branch.Id);
        _divisionA = _env.AddDivision(_class, "A");
        _divisionB = _env.AddDivision(_class, "B");
        _year = new AcademicYear { Id = Guid.NewGuid(), YearName = "2026-27", StartDate = new DateOnly(2026, 6, 1), EndDate = new DateOnly(2027, 4, 30), IsActive = true };
        _student = new Student
        {
            Id = Guid.NewGuid(), FirstName = "Riya", BranchId = branch.Id, ClassId = _class.Id, DivisionId = _divisionA.Id,
            StudentStatus = StudentStatuses.Active, IsActive = true, AdmissionDate = new DateOnly(2026, 6, 15),
        };
        _env.Db.AcademicYears.Add(_year);
        _env.Db.Students.Add(_student);
        _env.Db.SaveChanges();

        var db = _env.Db;
        var dateLocks = new AttendanceDateLockRepository(db, TestEnvironment.Logger<AttendanceDateLockRepository>());
        var yearLocks = new AttendanceAcademicYearLockRepository(db, TestEnvironment.Logger<AttendanceAcademicYearLockRepository>());
        var years = new AcademicYearRepository(db, TestEnvironment.Logger<AcademicYearRepository>());
        var students = new StudentRepository(db, TestEnvironment.Logger<StudentRepository>());
        var policy = new AttendancePolicy(students, dateLocks, yearLocks, years, new FixedClock(new DateTimeOffset(Today.ToDateTime(new TimeOnly(10, 0)), TimeSpan.Zero)));

        _service = new AttendanceService(new AttendanceServiceDependencies(
            new AttendanceRepository(db, TestEnvironment.Logger<AttendanceRepository>()), students, years,
            Substitute.For<IStudentResultRepository>(), Substitute.For<ILeaveRequestRepository>(),
            Substitute.For<IAttendanceCorrectionRequestRepository>(), dateLocks, yearLocks,
            new BranchRepository(db, TestEnvironment.Logger<BranchRepository>()),
            new ClassRepository(db, TestEnvironment.Logger<ClassRepository>()),
            new DivisionRepository(db, TestEnvironment.Logger<DivisionRepository>()),
            new SchoolHolidayRepository(db, TestEnvironment.Logger<SchoolHolidayRepository>()),
            Substitute.For<IClassTeacherMapRepository>(), Substitute.For<IStaffRepository>(),
            _env.Mapper, _env.Cache, Substitute.For<IUnitOfWork>(), policy), TestEnvironment.Logger<AttendanceService>());
    }

    private MarkAttendanceDto Mark(DateOnly date, string status = "P", Guid? studentId = null) =>
        new() { StudentId = studentId ?? _student.Id, AttendanceDate = date, Status = status };

    private void SetStudent(Action<Student> change)
    {
        change(_student);
        _env.Db.SaveChanges();
        _env.Db.ChangeTracker.Clear();
    }

    [Fact]
    public async Task Marking_stores_the_canonical_status_and_the_students_class()
    {
        var marked = await _service.MarkAttendanceAsync(Mark(Today, "present"));

        var saved = (await _env.Db.Attendances.FindAsync(marked.Id))!;
        Assert.Equal(AttendanceStatuses.Present, saved.Status);
        Assert.Equal(_class.Id, saved.ClassId);
        Assert.Equal(_divisionA.Id, saved.DivisionId);
    }

    [Fact]
    public async Task Unknown_statuses_future_dates_and_dates_before_admission_are_rejected()
    {
        await ErrorAssert.BadRequestAsync(() => _service.MarkAttendanceAsync(Mark(Today, "excused")));
        await ErrorAssert.BadRequestAsync(() => _service.MarkAttendanceAsync(Mark(Today.AddDays(5))));
        await ErrorAssert.BadRequestAsync(() => _service.MarkAttendanceAsync(Mark(new DateOnly(2026, 6, 1))));
    }

    [Fact]
    public async Task A_student_of_another_division_cannot_be_marked_in_this_one()
    {
        var dto = Mark(Today);
        dto.ClassId = _class.Id;
        dto.DivisionId = _divisionB.Id;

        await ErrorAssert.BadRequestAsync(() => _service.MarkAttendanceAsync(dto));
    }

    [Fact]
    public async Task Students_who_left_can_only_be_marked_up_to_their_leaving_date()
    {
        SetStudent(s => { s.StudentStatus = StudentStatuses.Dropped; s.IsActive = false; s.LeavingDate = new DateOnly(2026, 10, 31); });

        await _service.MarkAttendanceAsync(Mark(new DateOnly(2026, 10, 30)));       // correction before leaving: fine
        await ErrorAssert.ConflictAsync(() => _service.MarkAttendanceAsync(Mark(Today)));
    }

    [Fact]
    public async Task A_locked_day_cannot_be_marked_edited_or_deleted()
    {
        var marked = await _service.MarkAttendanceAsync(Mark(Today, "A"));
        await _service.LockAttendanceByDateAsync(_class.Id, _divisionA.Id, Today.ToDateTime(TimeOnly.MinValue));

        await ErrorAssert.ConflictAsync(() => _service.MarkAttendanceAsync(Mark(Today, "P")));
        await ErrorAssert.ConflictAsync(() => _service.UpdateAttendanceAsync(marked.Id, new UpdateAttendanceDto { Status = "P" }));
        await ErrorAssert.ConflictAsync(() => _service.SoftDeleteAttendanceAsync(marked.Id));
        Assert.True(await _service.ValidateAttendanceIsLockedAsync(_class.Id, Today.ToDateTime(TimeOnly.MinValue)));
    }

    [Fact]
    public async Task A_locked_academic_year_blocks_every_day_in_it()
    {
        await _service.LockAttendanceForAcademicYearAsync(_year.Id);

        await ErrorAssert.ConflictAsync(() => _service.MarkAttendanceAsync(Mark(Today)));
    }

    [Fact]
    public async Task A_class_register_is_all_or_nothing()
    {
        var left = new Student { Id = Guid.NewGuid(), FirstName = "Old", ClassId = _class.Id, DivisionId = _divisionA.Id, StudentStatus = StudentStatuses.Transferred, IsActive = false, LeavingDate = new DateOnly(2026, 9, 1) };
        _env.Db.Students.Add(left);
        _env.Db.SaveChanges();

        var register = new List<StudentAttendanceDto>
        {
            new() { StudentId = _student.Id, Status = "P" },
            new() { StudentId = left.Id, Status = "P" },
        };
        await ErrorAssert.ConflictAsync(() => _service.MarkClassAttendanceAsync(_class.Id, _divisionA.Id, Today.ToDateTime(TimeOnly.MinValue), register));

        Assert.Empty(_env.Db.Attendances); // the valid first entry was not written either
    }

    [Fact]
    public async Task The_same_student_twice_in_one_batch_is_rejected()
    {
        var batch = new BulkAttendanceDto
        {
            AttendanceDate = Today,
            Entries = new List<MarkAttendanceDto> { new() { StudentId = _student.Id, Status = "P" }, new() { StudentId = _student.Id, Status = "A" } },
        };

        await ErrorAssert.BadRequestAsync(() => _service.BulkMarkAttendanceAsync(batch));
    }

    public void Dispose() => _env.Dispose();
}
