using Application.ViewModels.ExamAnalytics;
using BonzerSchool.UnitTests.TestSupport;
using Domain.Entities.Academics;
using Domain.Entities.Exams;
using Domain.Entities.Students;
using Infrastructure.Services.Exams;

namespace BonzerSchool.UnitTests.Exams;

/// <summary>Maths in Class 5: Asha teaches division A, Ravi division B. A scored 80 and 60, B scored 40 and 20 (pass 35).</summary>
public class TeacherAnalyticsTests : IDisposable
{
    private readonly TestEnvironment _env = new();
    private readonly ExamAnalyticsService _service;
    private readonly Exam _exam;
    private readonly Guid _asha;
    private readonly Guid _ravi;

    public TeacherAnalyticsTests()
    {
        var branch = _env.AddBranch();
        var cls = _env.AddClass(branch.Id);
        var a = _env.AddDivision(cls, "A");
        var b = _env.AddDivision(cls, "B");
        var maths = _env.AddSubject();
        _asha = _env.AddStaff(branch.Id, "Asha").Id;
        _ravi = _env.AddStaff(branch.Id, "Ravi").Id;

        _exam = new Exam { Id = Guid.NewGuid(), Name = "Term 1", ClassId = cls.Id, StartDate = new DateOnly(2026, 10, 1) };
        var examSubject = new ExamSubject { Id = Guid.NewGuid(), ExamId = _exam.Id, SubjectId = maths.Id, TotalMaxMarks = 100, TotalPassMarks = 35 };
        var paper = new ExamSubjectComponent { Id = Guid.NewGuid(), ExamSubjectId = examSubject.Id, MaxMarks = 100 };
        _env.Db.AddRange(_exam, examSubject, paper,
            new TeacherSubjectMap { Id = Guid.NewGuid(), StaffId = _asha, SubjectId = maths.Id, ClassId = cls.Id, DivisionId = a.Id },
            new TeacherSubjectMap { Id = Guid.NewGuid(), StaffId = _ravi, SubjectId = maths.Id, ClassId = cls.Id, DivisionId = b.Id });
        foreach (var (division, marks) in new[] { (a, 80m), (a, 60m), (b, 40m), (b, 20m) })
        {
            var student = new Student { Id = Guid.NewGuid(), FirstName = "S", ClassId = cls.Id, DivisionId = division.Id };
            _env.Db.AddRange(student, new Mark { Id = Guid.NewGuid(), StudentId = student.Id, ExamSubjectComponentId = paper.Id, MarksObtained = marks });
        }
        _env.Db.SaveChanges();
        _service = new ExamAnalyticsService(_env.Db, TestEnvironment.Logger<ExamAnalyticsService>());
    }

    [Fact]
    public async Task A_teacher_is_measured_on_their_own_students_only()
    {
        var asha = Assert.Single(await _service.GetTeacherExamPerformanceAsync(_asha, new TeacherExamFilterDto()));

        Assert.Equal((2, 2, 0, 70m), (asha.StudentCount, asha.PassedCount, asha.FailedCount, asha.AveragePercentage!.Value));
    }

    [Fact]
    public async Task Teachers_are_compared_on_the_same_exam()
    {
        var comparison = await _service.CompareTeachersAsync(new TeacherComparisonFilterDto { ExamId = _exam.Id });

        Assert.Equal(new[] { "Asha Patil", "Ravi Patil" }, comparison.Select(c => c.TeacherName));
        Assert.Equal((30m, 1, 1), (comparison[1].AveragePercentage!.Value, comparison[1].PassedCount, comparison[1].FailedCount));
    }

    [Fact]
    public async Task Subject_performance_lists_each_exam()
    {
        var subject = Assert.Single(await _service.GetTeacherSubjectPerformanceAsync(_asha, new TeacherSubjectFilterDto()));

        var point = Assert.Single(subject.Points);
        Assert.Equal((70m, 100m), (point.AverageMarks!.Value, point.MaxMarks!.Value));
    }

    public void Dispose() => _env.Dispose();
}
