using Application.Abstractions;
using Application.Exams;
using Application.Students;
using Application.ViewModels.GradeMaster;
using Application.ViewModels.Mark;
using BonzerSchool.UnitTests.TestSupport;
using Domain.Entities.Exams;
using Domain.Entities.Organization;
using Domain.Entities.Students;
using Infrastructure.Persistence.Repositories.Exams;
using Infrastructure.Persistence.Repositories.Students;
using Infrastructure.Services.Exams;

namespace BonzerSchool.UnitTests.Exams;

public class ExamServiceTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 11, 3, 6, 0, 0, TimeSpan.Zero);
    private readonly TestEnvironment _env = new();
    private readonly MarkService _marks;
    private readonly StudentResultService _results;
    private readonly GradeMasterService _grades;
    private readonly Exam _exam;
    private readonly ExamSubjectComponent _theory;
    private readonly Student _asha;
    private readonly Student _ravi;

    public ExamServiceTests()
    {
        var db = _env.Db;
        var branch = _env.AddBranch();
        var cls = _env.AddClass(branch.Id);
        var year = new AcademicYear { Id = Guid.NewGuid(), YearName = "2026-27" };
        _exam = new Exam { Id = Guid.NewGuid(), Name = "Term 1", ClassId = cls.Id, BranchId = branch.Id, AcademicYearId = year.Id, StartDate = new DateOnly(2026, 10, 1), EndDate = new DateOnly(2026, 10, 10) };
        var maths = new ExamSubject { Id = Guid.NewGuid(), ExamId = _exam.Id, SubjectId = _env.AddSubject().Id, TotalMaxMarks = 100, TotalPassMarks = 35 };
        _theory = new ExamSubjectComponent { Id = Guid.NewGuid(), ExamSubjectId = maths.Id, MaxMarks = 100, PassMarks = 35 };
        _asha = new Student { Id = Guid.NewGuid(), FirstName = "Asha", ClassId = cls.Id, AcademicYearId = year.Id, StudentStatus = StudentStatuses.Active };
        _ravi = new Student { Id = Guid.NewGuid(), FirstName = "Ravi", ClassId = cls.Id, AcademicYearId = year.Id, StudentStatus = StudentStatuses.Active };
        db.AddRange(year, _exam, maths, _theory, _asha, _ravi);
        db.SaveChanges();

        var structure = new ExamStructure(new ExamRepository(db, TestEnvironment.Logger<ExamRepository>()),
            new ExamSubjectRepository(db, TestEnvironment.Logger<ExamSubjectRepository>()),
            new ExamSubjectComponentRepository(db, TestEnvironment.Logger<ExamSubjectComponentRepository>()));
        var markRepository = new MarkRepository(db, TestEnvironment.Logger<MarkRepository>());
        var gradeRepository = new GradeMasterRepository(db, TestEnvironment.Logger<GradeMasterRepository>());
        _marks = new MarkService(TestEnvironment.Logger<MarkService>(), markRepository, Substitute.For<IUnitOfWork>(), _env.Mapper, structure,
            new StudentRepository(db, TestEnvironment.Logger<StudentRepository>()), new FixedClock(Now));
        _results = new StudentResultService(TestEnvironment.Logger<StudentResultService>(),
            new StudentResultRepository(db, TestEnvironment.Logger<StudentResultRepository>()), _env.Mapper, structure,
            new ExamSubjectRepository(db, TestEnvironment.Logger<ExamSubjectRepository>()),
            new ExamSubjectComponentRepository(db, TestEnvironment.Logger<ExamSubjectComponentRepository>()), markRepository, gradeRepository, new FixedClock(Now));
        _grades = new GradeMasterService(TestEnvironment.Logger<GradeMasterService>(), gradeRepository, _env.Mapper, _env.Cache);
    }

    private MarkRequest Mark(Student student, decimal? marks, bool absent = false) =>
        new() { ExamSubjectComponentId = _theory.Id, StudentId = student.Id, MarksObtained = marks, IsAbsent = absent };

    [Fact]
    public async Task Marks_must_be_within_the_component_maximum()
    {
        await ErrorAssert.BadRequestAsync(() => _marks.CreateAsync(Mark(_asha, 101)));
        await ErrorAssert.BadRequestAsync(() => _marks.CreateAsync(Mark(_asha, -1)));
        await ErrorAssert.BadRequestAsync(() => _marks.CreateAsync(Mark(_asha, null)));

        var entered = await _marks.CreateAsync(Mark(_asha, 100));
        Assert.Equal(Now.UtcDateTime, (await _env.Db.Marks.FindAsync(entered.Id))!.EnteredAt);
    }

    [Fact]
    public async Task Absent_students_have_no_marks_and_marks_are_entered_once()
    {
        await ErrorAssert.BadRequestAsync(() => _marks.CreateAsync(Mark(_asha, 40, absent: true)));

        await _marks.CreateAsync(Mark(_asha, null, absent: true));
        await ErrorAssert.ConflictAsync(() => _marks.CreateAsync(Mark(_asha, 50)));
    }

    [Fact]
    public async Task A_locked_exam_rejects_marks()
    {
        _exam.IsLocked = true;
        await _env.Db.SaveChangesAsync();

        await ErrorAssert.ConflictAsync(() => _marks.CreateAsync(Mark(_asha, 50)));
    }

    [Fact]
    public async Task Students_outside_the_exam_class_or_off_the_roll_cannot_get_marks()
    {
        var otherClass = _env.AddClass(_env.AddBranch("City").Id, "Class 9");
        var outsider = new Student { Id = Guid.NewGuid(), FirstName = "X", ClassId = otherClass.Id, AcademicYearId = _exam.AcademicYearId, StudentStatus = StudentStatuses.Active };
        var dropped = new Student { Id = Guid.NewGuid(), FirstName = "Y", ClassId = _exam.ClassId, AcademicYearId = _exam.AcademicYearId, StudentStatus = StudentStatuses.Dropped, LeavingDate = new DateOnly(2026, 9, 1) };
        _env.Db.AddRange(outsider, dropped);
        await _env.Db.SaveChangesAsync();

        await ErrorAssert.BadRequestAsync(() => _marks.CreateAsync(Mark(outsider, 50)));
        await ErrorAssert.ConflictAsync(() => _marks.CreateAsync(Mark(dropped, 50)));
    }

    [Fact]
    public async Task Results_are_generated_from_marks_and_regenerating_replaces_them()
    {
        await _grades.CreateAsync(new GradeMasterRequest { Grade = "A", MinPercent = 75, MaxPercent = 100 });
        await _grades.CreateAsync(new GradeMasterRequest { Grade = "B", MinPercent = 35, MaxPercent = 74.99m });
        await _marks.CreateAsync(Mark(_asha, 80));
        var raviMark = await _marks.CreateAsync(Mark(_ravi, 30));

        var results = await _results.GenerateResultsAsync(_exam.Id);
        Assert.Equal(2, results.Count);
        var asha = results.Single(r => r.StudentId == _asha.Id);
        Assert.Equal(("A", ResultCalculator.Pass, 1), (asha.Grade, asha.Result, asha.Rank));
        Assert.Equal(ResultCalculator.Fail, results.Single(r => r.StudentId == _ravi.Id).Result);

        await _marks.UpdateAsync(raviMark.Id, Mark(_ravi, 90));
        await _results.GenerateResultsAsync(_exam.Id);
        Assert.Equal(2, _env.Db.StudentResults.Count());
        Assert.Equal(1, _env.Db.StudentResults.Single(r => r.StudentId == _ravi.Id).Rank);
    }

    [Fact]
    public async Task Grade_bands_cannot_overlap()
    {
        await _grades.CreateAsync(new GradeMasterRequest { Grade = "A", MinPercent = 75, MaxPercent = 100 });

        await ErrorAssert.ConflictAsync(() => _grades.CreateAsync(new GradeMasterRequest { Grade = "B", MinPercent = 60, MaxPercent = 75 }));
        await ErrorAssert.ConflictAsync(() => _grades.CreateAsync(new GradeMasterRequest { Grade = "a", MinPercent = 0, MaxPercent = 10 }));
        await _grades.CreateAsync(new GradeMasterRequest { Grade = "B", MinPercent = 60, MaxPercent = 74.99m });
    }

    public void Dispose() => _env.Dispose();
}
