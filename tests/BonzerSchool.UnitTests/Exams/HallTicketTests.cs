using Application.Students;
using Application.ViewModels.Certificates;
using BonzerSchool.UnitTests.TestSupport;
using Domain.Entities.Exams;
using Domain.Entities.Organization;
using Domain.Entities.Students;
using Infrastructure.Persistence.Repositories.Academics;
using Infrastructure.Persistence.Repositories.Exams;
using Infrastructure.Persistence.Repositories.Organization;
using Infrastructure.Persistence.Repositories.Students;
using Infrastructure.Services.Certificates;
using Infrastructure.Services.Common;
using Infrastructure.Services.Exams;
using Microsoft.Extensions.Caching.Memory;

namespace BonzerSchool.UnitTests.Exams;

public class HallTicketTests : IDisposable
{
    private readonly TestEnvironment _env = new();
    private readonly HallTicketService _service;
    private readonly Exam _exam;
    private readonly AcademicYear _year;
    private readonly Domain.Entities.Academics.Class _class;

    public HallTicketTests()
    {
        var db = _env.Db;
        _class = _env.AddClass(_env.AddBranch().Id);
        _year = new AcademicYear { Id = Guid.NewGuid(), YearName = "2026-27" };
        _exam = new Exam { Id = Guid.NewGuid(), Name = "Term 1", ClassId = _class.Id, AcademicYearId = _year.Id, StartDate = new DateOnly(2026, 10, 1) };
        db.AddRange(_year, _exam);
        db.SaveChanges();
        var hallTickets = new HallTicketRepository(db, TestEnvironment.Logger<HallTicketRepository>());
        _service = new HallTicketService(hallTickets, new StudentRepository(db, TestEnvironment.Logger<StudentRepository>()),
            new ExamRepository(db, TestEnvironment.Logger<ExamRepository>()), new ExamSubjectRepository(db, TestEnvironment.Logger<ExamSubjectRepository>()),
            new SubjectRepository(db, TestEnvironment.Logger<SubjectRepository>()), new AcademicYearRepository(db, TestEnvironment.Logger<AcademicYearRepository>()),
            new ClassRepository(db, TestEnvironment.Logger<ClassRepository>()), new DivisionRepository(db, TestEnvironment.Logger<DivisionRepository>()),
            _env.Mapper, TestEnvironment.Logger<HallTicketService>(), new CacheService(new MemoryCache(new MemoryCacheOptions()), _env.TenantContext),
            new HallTicketNumberGenerator(hallTickets));
    }

    private Student AddStudent(string status = StudentStatuses.Active, Guid? classId = null)
    {
        var student = new Student { Id = Guid.NewGuid(), FirstName = "S", AdmissionNo = Guid.NewGuid().ToString("N")[..6], ClassId = classId ?? _class.Id, AcademicYearId = _year.Id, StudentStatus = status };
        _env.Db.Students.Add(student);
        _env.Db.SaveChanges();
        return student;
    }

    [Fact]
    public async Task Only_students_who_sit_the_exam_get_a_ticket()
    {
        var other = _env.AddClass(_class.BranchId!.Value, "Class 9");

        await ErrorAssert.ConflictAsync(() => _service.GenerateAsync(new HallTicketRequest { StudentId = AddStudent(StudentStatuses.Dropped).Id, ExamId = _exam.Id }));
        await ErrorAssert.BadRequestAsync(() => _service.GenerateAsync(new HallTicketRequest { StudentId = AddStudent(classId: other.Id).Id, ExamId = _exam.Id }));
        var ticket = await _service.GenerateAsync(new HallTicketRequest { StudentId = AddStudent().Id, ExamId = _exam.Id, SeatNumber = " A-12 " });
        Assert.Equal("A-12", ticket.SeatNumber);
    }

    [Fact]
    public async Task A_seat_is_given_once_per_exam()
    {
        await _service.GenerateAsync(new HallTicketRequest { StudentId = AddStudent().Id, ExamId = _exam.Id, SeatNumber = "A-12" });

        await ErrorAssert.ConflictAsync(() => _service.GenerateAsync(new HallTicketRequest { StudentId = AddStudent().Id, ExamId = _exam.Id, SeatNumber = "A-12" }));
    }

    public void Dispose() => _env.Dispose();
}
