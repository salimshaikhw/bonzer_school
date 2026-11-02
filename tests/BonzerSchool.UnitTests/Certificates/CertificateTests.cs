using Application.Abstractions.Repositories.Certificates;
using Application.Abstractions.Services.Certificates;
using Application.Students;
using Application.ViewModels.Certificates;
using BonzerSchool.UnitTests.TestSupport;
using Domain.Entities.Certificates;
using Domain.Entities.Organization;
using Domain.Entities.Students;
using Infrastructure.Persistence.Repositories.Academics;
using Infrastructure.Persistence.Repositories.Certificates;
using Infrastructure.Persistence.Repositories.Organization;
using Infrastructure.Persistence.Repositories.Students;
using Infrastructure.Services.Certificates;
using Infrastructure.Services.Certificates.TcWorkflow;
using Infrastructure.Services.Common;
using Microsoft.Extensions.Caching.Memory;

namespace BonzerSchool.UnitTests.Certificates;

public class TcIssueHandlerTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 11, 3, 9, 0, 0, TimeSpan.Zero);
    private readonly TestEnvironment _env = new();
    private readonly ITcCertificateRepository _tcs = Substitute.For<ITcCertificateRepository>();
    private readonly IssueTcHandler _handler;

    public TcIssueHandlerTests()
    {
        _handler = new IssueTcHandler(_tcs, new StudentRepository(_env.Db, TestEnvironment.Logger<StudentRepository>()),
            new FixedClock(Now), TestEnvironment.Logger<IssueTcHandler>());
    }

    private (Student Student, TcCertificate Tc) Setup(string status, bool duplicate = false)
    {
        var student = new Student { Id = Guid.NewGuid(), FirstName = "Riya", StudentStatus = status, IsActive = status == StudentStatuses.Active };
        _env.Db.Students.Add(student);
        _env.Db.SaveChanges();
        _env.Db.ChangeTracker.Clear();
        var tc = new TcCertificate { Id = Guid.NewGuid(), StudentId = student.Id, TcNumber = "TC/2026-27/5/0001", IsDuplicate = duplicate, IssueDate = new DateOnly(2026, 11, 2) };
        _tcs.GetByIdAsync(tc.Id).Returns(tc);
        return (student, tc);
    }

    private Student Reload(Guid id)
    {
        _env.Db.ChangeTracker.Clear();
        return _env.Db.Students.Find(id)!;
    }

    [Theory]
    [InlineData(StudentStatuses.Active)]
    [InlineData(StudentStatuses.Dropped)]
    [InlineData(StudentStatuses.PassedOut)]
    public async Task Issuing_an_original_tc_makes_the_student_leave(string status)
    {
        var (student, tc) = Setup(status);

        await _handler.HandleAsync(new TcApprovalRequest { TcId = tc.Id, StaffId = Guid.NewGuid(), Action = "ISSUE" });

        var saved = Reload(student.Id);
        Assert.Equal(StudentStatuses.Transferred, saved.StudentStatus);
        Assert.True(saved.IsTcIssued);
        Assert.Equal(new DateOnly(2026, 11, 2), saved.LeavingDate);
        await _tcs.Received(1).ExecuteSqlRawAsync(Arg.Any<string>(), Arg.Any<object[]>());
    }

    [Fact]
    public async Task A_student_who_already_left_blocks_the_issue_before_anything_changes()
    {
        var (student, tc) = Setup(StudentStatuses.Transferred);

        await ErrorAssert.ConflictAsync(() => _handler.HandleAsync(new TcApprovalRequest { TcId = tc.Id, StaffId = Guid.NewGuid(), Action = "ISSUE" }));

        await _tcs.DidNotReceive().ExecuteSqlRawAsync(Arg.Any<string>(), Arg.Any<object[]>());
        Assert.Equal(StudentStatuses.Transferred, Reload(student.Id).StudentStatus);
    }

    [Fact]
    public async Task A_duplicate_tc_does_not_touch_the_student()
    {
        var (student, tc) = Setup(StudentStatuses.Transferred, duplicate: true);

        await _handler.HandleAsync(new TcApprovalRequest { TcId = tc.Id, StaffId = Guid.NewGuid(), Action = "ISSUE" });

        await _tcs.Received(1).ExecuteSqlRawAsync(Arg.Any<string>(), Arg.Any<object[]>());
        Assert.Equal(StudentStatuses.Transferred, Reload(student.Id).StudentStatus);
    }

    public void Dispose() => _env.Dispose();
}

public class CertificateRequestTests : IDisposable
{
    private readonly TestEnvironment _env = new();
    private readonly TcCertificateService _tcService;
    private readonly BonafideCertificateService _bonafideService;
    private readonly Student _student;

    public CertificateRequestTests()
    {
        var db = _env.Db;
        var cls = _env.AddClass(_env.AddBranch().Id);
        var year = new AcademicYear { Id = Guid.NewGuid(), YearName = "2026-27" };
        _student = new Student { Id = Guid.NewGuid(), FirstName = "Riya", ClassId = cls.Id, AcademicYearId = year.Id, StudentStatus = StudentStatuses.Active };
        db.AddRange(year, _student);
        db.SaveChanges();

        var tcs = new TcCertificateRepository(db, TestEnvironment.Logger<TcCertificateRepository>());
        var students = new StudentRepository(db, TestEnvironment.Logger<StudentRepository>());
        var years = new AcademicYearRepository(db, TestEnvironment.Logger<AcademicYearRepository>());
        var classes = new ClassRepository(db, TestEnvironment.Logger<ClassRepository>());
        var divisions = new DivisionRepository(db, TestEnvironment.Logger<DivisionRepository>());
        var cache = new CacheService(new MemoryCache(new MemoryCacheOptions()), _env.TenantContext);
        _tcService = new TcCertificateService(tcs, students, years, classes, divisions, _env.Mapper, TestEnvironment.Logger<TcCertificateService>(), cache,
            new TcNumberGenerator(tcs, years, classes),
            new HeadClerkApprovalHandler(tcs, TestEnvironment.Logger<HeadClerkApprovalHandler>()),
            new ClassTeacherApprovalHandler(tcs, TestEnvironment.Logger<ClassTeacherApprovalHandler>()),
            new PrincipalApprovalHandler(tcs, TestEnvironment.Logger<PrincipalApprovalHandler>()),
            new IssueTcHandler(tcs, students, TimeProvider.System, TestEnvironment.Logger<IssueTcHandler>()),
            new RejectTcHandler(tcs, TestEnvironment.Logger<RejectTcHandler>()));
        var bonafides = new BonafideCertificateRepository(db, TestEnvironment.Logger<BonafideCertificateRepository>());
        _bonafideService = new BonafideCertificateService(bonafides, students, years, classes, divisions, _env.Mapper,
            TestEnvironment.Logger<BonafideCertificateService>(), cache, new BonafideNumberGenerator(bonafides, years, classes));
    }

    [Fact]
    public async Task One_original_tc_per_student_and_duplicates_need_an_issued_original()
    {
        var first = await _tcService.CreateAsync(new TcCertificateRequest { StudentId = _student.Id, Reason = "Relocation" });
        Assert.StartsWith("TC/2026-27/", first.TcNumber);

        await ErrorAssert.ConflictAsync(() => _tcService.CreateAsync(new TcCertificateRequest { StudentId = _student.Id }));
        await ErrorAssert.ConflictAsync(() => _tcService.CreateAsync(new TcCertificateRequest { StudentId = _student.Id, IsDuplicate = true }));

        var original = _env.Db.TcCertificates.Single();
        original.Status = "ISSUED";
        await _env.Db.SaveChangesAsync();
        var copy = await _tcService.CreateAsync(new TcCertificateRequest { StudentId = _student.Id, IsDuplicate = true });
        Assert.True(copy.IsDuplicate);
    }

    [Fact]
    public async Task A_rejected_request_does_not_block_a_new_one()
    {
        await _tcService.CreateAsync(new TcCertificateRequest { StudentId = _student.Id });
        _env.Db.TcCertificates.Single().Status = "REJECTED";
        await _env.Db.SaveChangesAsync();

        await _tcService.CreateAsync(new TcCertificateRequest { StudentId = _student.Id });
        Assert.Equal(2, _env.Db.TcCertificates.Count());
    }

    [Fact]
    public async Task Bonafide_certificates_are_only_for_current_students()
    {
        await _bonafideService.CreateAsync(new BonafideCertificateRequest { StudentId = _student.Id, Purpose = "Bank account" });

        _student.StudentStatus = StudentStatuses.PassedOut;
        await _env.Db.SaveChangesAsync();
        await ErrorAssert.ConflictAsync(() => _bonafideService.CreateAsync(new BonafideCertificateRequest { StudentId = _student.Id }));
    }

    public void Dispose() => _env.Dispose();
}
