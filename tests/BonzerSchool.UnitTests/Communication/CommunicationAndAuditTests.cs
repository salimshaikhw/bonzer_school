using Application.Abstractions;
using Application.ViewModels.AuditLog;
using Application.ViewModels.Notification;
using BonzerSchool.UnitTests.TestSupport;
using Domain.Entities.Students;
using Infrastructure.Persistence.Repositories.Academics;
using Infrastructure.Persistence.Repositories.Communication;
using Infrastructure.Persistence.Repositories.Students;
using Infrastructure.Services.Administration;
using Infrastructure.Services.Communication;

namespace BonzerSchool.UnitTests.Communication;

public class AuditLogServiceTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 11, 3, 9, 0, 0, TimeSpan.Zero);
    private readonly TestEnvironment _env = new();
    private readonly AuditLogService _service;

    public AuditLogServiceTests()
    {
        _service = new AuditLogService(_env.Db, _env.Mapper, TestEnvironment.Logger<AuditLogService>(), Substitute.For<IUnitOfWork>(), new FixedClock(Now));
    }

    [Fact]
    public async Task Entries_are_saved_with_the_server_time()
    {
        var entry = await _service.CreateAsync(new AuditLogRequest { Module = "Fees", ActionType = "DELETE", CreatedAt = new DateTime(2000, 1, 1) });

        _env.Db.ChangeTracker.Clear();
        var saved = await _env.Db.AuditLogs.FindAsync(entry.Id);
        Assert.NotNull(saved); // previously never saved
        Assert.Equal(Now.UtcDateTime, saved!.CreatedAt);
    }

    [Fact]
    public async Task Entries_cannot_be_changed_or_deleted()
    {
        var entry = await _service.CreateAsync(new AuditLogRequest { Module = "Fees", ActionType = "DELETE" });

        await ErrorAssert.ConflictAsync(() => _service.UpdateAsync(entry.Id, new AuditLogRequest { Module = "x", ActionType = "y" }));
        await ErrorAssert.ConflictAsync(() => _service.DeleteAsync(entry.Id));
        Assert.Single(_env.Db.AuditLogs);
    }

    [Fact]
    public async Task Unknown_entries_are_not_found()
    {
        await ErrorAssert.NotFoundAsync(() => _service.GetByIdAsync(Guid.NewGuid()));
    }

    public void Dispose() => _env.Dispose();
}

public class NotificationServiceTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 11, 3, 9, 0, 0, TimeSpan.Zero);
    private readonly TestEnvironment _env = new();
    private readonly NotificationService _service;
    private readonly Domain.Entities.Organization.Branch _branch;
    private readonly Domain.Entities.Academics.Division _division;
    private readonly Student _student;

    public NotificationServiceTests()
    {
        _branch = _env.AddBranch();
        _division = _env.AddDivision(_env.AddClass(_branch.Id));
        _student = new Student { Id = Guid.NewGuid(), FirstName = "Riya", BranchId = _branch.Id, DivisionId = _division.Id };
        _env.Db.Students.Add(_student);
        _env.Db.SaveChanges();
        _service = new NotificationService(new NotificationRepository(_env.Db, TestEnvironment.Logger<NotificationRepository>()),
            TestEnvironment.Logger<NotificationService>(), _env.Mapper, _env.AcademicStructure(),
            new DivisionRepository(_env.Db, TestEnvironment.Logger<DivisionRepository>()),
            new StudentRepository(_env.Db, TestEnvironment.Logger<StudentRepository>()), new FixedClock(Now));
    }

    [Fact]
    public async Task A_notification_records_when_it_was_sent()
    {
        var sent = await _service.CreateAsync(new NotificationRequest { StudentId = _student.Id, Title = " Fee reminder ", Message = "Due on 15th" });

        Assert.Equal("Fee reminder", sent.Title);
        Assert.Equal(Now.UtcDateTime, sent.SentAt);
    }

    [Fact]
    public async Task Targets_must_exist_and_fit_together()
    {
        var otherBranch = _env.AddBranch("City");

        await ErrorAssert.BadRequestAsync(() => _service.CreateAsync(new NotificationRequest { StudentId = Guid.NewGuid(), Title = "x", Message = "y" }));
        await ErrorAssert.BadRequestAsync(() => _service.CreateAsync(new NotificationRequest { DivisionId = _division.Id, BranchId = otherBranch.Id, Title = "x", Message = "y" }));
        await ErrorAssert.BadRequestAsync(() => _service.CreateAsync(new NotificationRequest { StudentId = _student.Id, BranchId = otherBranch.Id, Title = "x", Message = "y" }));
        await _service.CreateAsync(new NotificationRequest { Title = "Holiday tomorrow", Message = "School closed" }); // whole school
    }

    public void Dispose() => _env.Dispose();
}
