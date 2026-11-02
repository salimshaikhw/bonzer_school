using Application.Abstractions;
using Application.Abstractions.Repositories.Academics;
using Application.Abstractions.Repositories.Attendances;
using Application.Abstractions.Repositories.Exams;
using Application.Abstractions.Repositories.Fees;
using Application.Abstractions.Repositories.Personnel;
using Application.Attendances;
using Application.ViewModels.Attendance;
using BonzerSchool.UnitTests.TestSupport;
using Domain.Entities.Attendances;
using Domain.Entities.Students;
using Infrastructure.Persistence.Repositories.Academics;
using Infrastructure.Persistence.Repositories.Attendances;
using Infrastructure.Persistence.Repositories.Organization;
using Infrastructure.Persistence.Repositories.Students;
using Infrastructure.Services.Attendances;
using Infrastructure.Services.Common;
using Infrastructure.Services.Students;
using Microsoft.Extensions.Caching.Memory;

namespace BonzerSchool.UnitTests.Attendances;

public class AttendanceTallyTests
{
    [Fact]
    public void Late_counts_as_attended_and_a_half_day_as_half()
    {
        var tally = AttendanceTally.Of(new[] { "Present", "Late", "HalfDay", "Absent" });

        Assert.Equal(4, tally.WorkingDays);
        Assert.Equal(2.5m, tally.Attended);
        Assert.Equal(62.5m, tally.Percentage);
    }

    [Fact]
    public void Holidays_and_unknown_statuses_are_not_working_days()
    {
        var tally = AttendanceTally.Of(new[] { "P", "Holiday", null, "excused", "absent" });

        Assert.Equal(2, tally.WorkingDays);
        Assert.Equal(50m, tally.Percentage);
    }

    [Fact]
    public void No_working_days_is_zero_percent() => Assert.Equal(0, AttendanceTally.Of(Array.Empty<string>()).Percentage);
}

public class AttendanceReportServiceTests : IDisposable
{
    private readonly TestEnvironment _env = new();
    private readonly AttendanceReportService _reports;
    private readonly Student _student;

    public AttendanceReportServiceTests()
    {
        var branch = _env.AddBranch(); // Sunday off (default), no Saturday off
        var cls = _env.AddClass(branch.Id);
        _student = new Student { Id = Guid.NewGuid(), FirstName = "Riya", BranchId = branch.Id, ClassId = cls.Id };
        _env.Db.Students.Add(_student);
        _env.Db.SchoolHolidays.Add(new SchoolHoliday { Id = Guid.NewGuid(), BranchId = branch.Id, HolidayDate = new DateOnly(2026, 10, 30), Name = "Diwali", IsDeleted = false });

        // Mon 26 Oct .. Sun 1 Nov 2026
        void Mark(int day, string status) => _env.Db.Attendances.Add(new Attendance
        {
            Id = Guid.NewGuid(), StudentId = _student.Id, ClassId = cls.Id, AttendanceDate = day > 25 ? new DateOnly(2026, 10, day) : new DateOnly(2026, 11, day), Status = status, IsDeleted = false,
        });
        Mark(26, "Present");  // Mon
        Mark(27, "Late");     // Tue
        Mark(28, "HalfDay");  // Wed
        Mark(29, "Absent");   // Thu
        Mark(30, "Present");  // Fri: calendar holiday, not a working day
        Mark(31, "Present");  // Sat: working day
        Mark(1, "Present");   // Sun: weekly off, not a working day
        _env.Db.SaveChanges();

        var db = _env.Db;
        var students = new StudentRepository(db, TestEnvironment.Logger<StudentRepository>());
        var years = new AcademicYearRepository(db, TestEnvironment.Logger<AcademicYearRepository>());
        var dateLocks = new AttendanceDateLockRepository(db, TestEnvironment.Logger<AttendanceDateLockRepository>());
        var yearLocks = new AttendanceAcademicYearLockRepository(db, TestEnvironment.Logger<AttendanceAcademicYearLockRepository>());
        _reports = new AttendanceReportService(new AttendanceServiceDependencies(
            new AttendanceRepository(db, TestEnvironment.Logger<AttendanceRepository>()), students, years,
            Substitute.For<IStudentResultRepository>(), Substitute.For<ILeaveRequestRepository>(),
            Substitute.For<IAttendanceCorrectionRequestRepository>(), dateLocks, yearLocks,
            new BranchRepository(db, TestEnvironment.Logger<BranchRepository>()),
            new ClassRepository(db, TestEnvironment.Logger<ClassRepository>()),
            new DivisionRepository(db, TestEnvironment.Logger<DivisionRepository>()),
            new SchoolHolidayRepository(db, TestEnvironment.Logger<SchoolHolidayRepository>()),
            Substitute.For<IClassTeacherMapRepository>(), Substitute.For<IStaffRepository>(),
            _env.Mapper, _env.Cache, Substitute.For<IUnitOfWork>(),
            new AttendancePolicy(students, dateLocks, yearLocks, years, TimeProvider.System)), TestEnvironment.Logger<AttendanceReportService>());
    }

    [Fact]
    public async Task Weekly_offs_and_calendar_holidays_are_left_out_and_late_and_half_days_count()
    {
        var summary = await _reports.GetStudentAttendanceSummaryAsync(_student.Id, AttendancePeriodType.Custom,
            new DateTime(2026, 10, 26), new DateTime(2026, 11, 1));

        Assert.Equal(5, summary.TotalDays);          // Mon, Tue, Wed, Thu, Sat
        Assert.Equal(2, summary.PresentCount);
        Assert.Equal(1, summary.LateCount);
        Assert.Equal(70m, summary.Percentage);        // (2 + 1 + 0.5) / 5; the old formula reported 50 %
    }

    public void Dispose() => _env.Dispose();
}

public class StudentReadServiceTests : IDisposable
{
    private readonly TestEnvironment _env = new();
    private readonly StudentQueryService _queries;
    private readonly StudentAnalyticsService _analytics;
    private readonly Guid _branchId;

    public StudentReadServiceTests()
    {
        _branchId = _env.AddBranch().Id;
        var db = _env.Db;
        var deps = new StudentServiceDependencies(
            new StudentRepository(db, TestEnvironment.Logger<StudentRepository>()), Substitute.For<IUnitOfWork>(), _env.Mapper,
            new CacheService(new MemoryCache(new MemoryCacheOptions()), _env.TenantContext),
            Substitute.For<ITeacherSubjectMapRepository>(), Substitute.For<IInvoiceRepository>(),
            new AttendanceRepository(db, TestEnvironment.Logger<AttendanceRepository>()),
            new ClassRepository(db, TestEnvironment.Logger<ClassRepository>()),
            new DivisionRepository(db, TestEnvironment.Logger<DivisionRepository>()),
            new BranchRepository(db, TestEnvironment.Logger<BranchRepository>()),
            new AcademicYearRepository(db, TestEnvironment.Logger<AcademicYearRepository>()),
            _env.AcademicStructure(), TimeProvider.System);
        _queries = new StudentQueryService(deps, TestEnvironment.Logger<StudentQueryService>());
        _analytics = new StudentAnalyticsService(deps, TestEnvironment.Logger<StudentAnalyticsService>());
    }

    private Student AddStudent(string name, string gender)
    {
        var student = new Student { Id = Guid.NewGuid(), FirstName = name, Gender = gender, BranchId = _branchId, IsActive = true, StudentStatus = "ACTIVE" };
        _env.Db.Students.Add(student);
        _env.Db.SaveChanges();
        return student;
    }

    private void Attendance(Student student, string status, bool holiday = false) => _env.Db.Attendances.Add(new Attendance
    {
        Id = Guid.NewGuid(), StudentId = student.Id, AttendanceDate = DateOnly.FromDateTime(DateTime.UtcNow), Status = status, IsHoliday = holiday, IsDeleted = false,
    });

    [Fact]
    public async Task Low_attendance_ignores_holiday_records()
    {
        var regular = AddStudent("Asha", "Female");
        var absentee = AddStudent("Ravi", "Male");
        Attendance(regular, "Present");
        Attendance(regular, "Absent", holiday: true);   // used to count as an absence (50 %)
        Attendance(absentee, "Absent");
        await _env.Db.SaveChangesAsync();

        var low = (await _queries.GetStudentsWithLowAttendanceAsync(75)).Select(s => s.Id).ToList();

        Assert.Equal(new[] { absentee.Id }, low);
    }

    [Fact]
    public async Task Gender_distribution_counts_students_of_the_branch()
    {
        AddStudent("Asha", "Female");
        AddStudent("Neha", "Female");
        AddStudent("Ravi", "Male");

        var distribution = (await _analytics.GetGenderDistributionAsync(_branchId)).ToDictionary(d => d.Key, d => d.Count);

        Assert.Equal(2, distribution["Female"]);
        Assert.Equal(1, distribution["Male"]);
    }

    public void Dispose() => _env.Dispose();
}
