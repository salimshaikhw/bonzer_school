using Application.ViewModels.ClassSubjectMap;
using Application.ViewModels.ClassTeacherMap;
using Application.ViewModels.Homework;
using Application.ViewModels.Subject;
using Application.ViewModels.TeacherSubjectMap;
using Application.ViewModels.Timetable;
using BonzerSchool.UnitTests.TestSupport;
using Domain.Entities.Academics;
using Domain.Entities.Personnel;
using Infrastructure.Persistence.Repositories.Academics;
using Infrastructure.Services.Academics;

namespace BonzerSchool.UnitTests.Academics;

/// <summary>Shared setup: one branch with Class 5 (divisions A, B), Class 6 (division A), a subject and two teachers.</summary>
public abstract class AcademicsTestBase : IDisposable
{
    protected readonly TestEnvironment Env = new();
    protected readonly Class Class5;
    protected readonly Class Class6;
    protected readonly Division Class5A;
    protected readonly Division Class5B;
    protected readonly Division Class6A;
    protected readonly Subject Maths;
    protected readonly Staff Teacher;
    protected readonly Staff OtherTeacher;

    protected AcademicsTestBase()
    {
        var branch = Env.AddBranch();
        Class5 = Env.AddClass(branch.Id, "Class 5");
        Class6 = Env.AddClass(branch.Id, "Class 6");
        Class5A = Env.AddDivision(Class5, "A");
        Class5B = Env.AddDivision(Class5, "B");
        Class6A = Env.AddDivision(Class6, "A");
        Maths = Env.AddSubject();
        Teacher = Env.AddStaff(branch.Id);
        OtherTeacher = Env.AddStaff(branch.Id, "Ravi");
    }

    public void Dispose() => Env.Dispose();
}

public class SubjectServiceTests : AcademicsTestBase
{
    private readonly SubjectService _service;

    public SubjectServiceTests()
    {
        _service = new SubjectService(new SubjectRepository(Env.Db, TestEnvironment.Logger<SubjectRepository>()),
            TestEnvironment.Logger<SubjectService>(), Env.Mapper, Env.Cache);
    }

    [Fact]
    public async Task Codes_are_stored_upper_case_and_must_be_unique()
    {
        var science = await _service.CreateAsync(new SubjectRequest { Name = "Science", Code = " sci " });
        Assert.Equal("SCI", science.Code);

        await ErrorAssert.ConflictAsync(() => _service.CreateAsync(new SubjectRequest { Name = "General Science", Code = "SCI" }));
        await ErrorAssert.ConflictAsync(() => _service.CreateAsync(new SubjectRequest { Name = "mathematics" }));
    }
}

public class ClassSubjectAndTeacherMapServiceTests : AcademicsTestBase
{
    private readonly ClassSubjectMapService _classSubjects;
    private readonly ClassTeacherMapService _classTeachers;
    private readonly TeacherSubjectMapService _teacherSubjects;

    public ClassSubjectAndTeacherMapServiceTests()
    {
        var structure = Env.AcademicStructure();
        _classSubjects = new ClassSubjectMapService(TestEnvironment.Logger<ClassSubjectMapService>(),
            new ClassSubjectMapRepository(Env.Db, TestEnvironment.Logger<ClassSubjectMapRepository>()), Env.Mapper, structure);
        _classTeachers = new ClassTeacherMapService(TestEnvironment.Logger<ClassTeacherMapService>(),
            new ClassTeacherMapRepository(Env.Db, TestEnvironment.Logger<ClassTeacherMapRepository>()), Env.Mapper, structure);
        _teacherSubjects = new TeacherSubjectMapService(TestEnvironment.Logger<TeacherSubjectMapService>(),
            new TeacherSubjectMapRepository(Env.Db, TestEnvironment.Logger<TeacherSubjectMapRepository>()), Env.Mapper, structure);
    }

    [Fact]
    public async Task A_subject_is_assigned_to_a_class_once_per_year()
    {
        var request = new ClassSubjectMapRequest { ClassId = Class5.Id, SubjectId = Maths.Id, AcademicYear = "2026-27" };
        await _classSubjects.CreateAsync(request);

        await ErrorAssert.ConflictAsync(() => _classSubjects.CreateAsync(request));
        await _classSubjects.CreateAsync(new ClassSubjectMapRequest { ClassId = Class5.Id, SubjectId = Maths.Id, AcademicYear = "2027-28" });
    }

    [Fact]
    public async Task A_division_of_another_class_is_rejected()
    {
        await ErrorAssert.BadRequestAsync(() => _classSubjects.CreateAsync(
            new ClassSubjectMapRequest { ClassId = Class5.Id, DivisionId = Class6A.Id, SubjectId = Maths.Id }));
    }

    [Fact]
    public async Task Unknown_subject_or_teacher_is_rejected()
    {
        await ErrorAssert.BadRequestAsync(() => _classSubjects.CreateAsync(
            new ClassSubjectMapRequest { ClassId = Class5.Id, SubjectId = Guid.NewGuid() }));
        await ErrorAssert.BadRequestAsync(() => _classTeachers.CreateAsync(
            new ClassTeacherMapRequest { ClassId = Class5.Id, StaffId = Guid.NewGuid() }));
    }

    [Fact]
    public async Task A_division_has_one_class_teacher_per_year_but_many_subject_teachers()
    {
        await _classTeachers.CreateAsync(new ClassTeacherMapRequest { ClassId = Class5.Id, DivisionId = Class5A.Id, StaffId = Teacher.Id, AcademicYear = "2026-27" });

        await ErrorAssert.ConflictAsync(() => _classTeachers.CreateAsync(
            new ClassTeacherMapRequest { ClassId = Class5.Id, DivisionId = Class5A.Id, StaffId = OtherTeacher.Id, AcademicYear = "2026-27" }));

        // A subject teacher in the same division is fine, and so is a class teacher for division B.
        await _classTeachers.CreateAsync(new ClassTeacherMapRequest { ClassId = Class5.Id, DivisionId = Class5A.Id, StaffId = OtherTeacher.Id, SubjectId = Maths.Id, AcademicYear = "2026-27" });
        await _classTeachers.CreateAsync(new ClassTeacherMapRequest { ClassId = Class5.Id, DivisionId = Class5B.Id, StaffId = OtherTeacher.Id, AcademicYear = "2026-27" });
    }

    [Fact]
    public async Task Teacher_subject_mappings_are_unique_and_need_a_class_for_a_division()
    {
        var request = new TeacherSubjectMapRequest { StaffId = Teacher.Id, SubjectId = Maths.Id, ClassId = Class5.Id, AcademicYear = "2026-27" };
        await _teacherSubjects.CreateAsync(request);

        await ErrorAssert.ConflictAsync(() => _teacherSubjects.CreateAsync(request));
        await ErrorAssert.BadRequestAsync(() => _teacherSubjects.CreateAsync(
            new TeacherSubjectMapRequest { StaffId = Teacher.Id, SubjectId = Maths.Id, DivisionId = Class5A.Id }));
    }
}

public class TimetableServiceTests : AcademicsTestBase
{
    private readonly TimetableService _service;
    private readonly Guid _yearId = Guid.NewGuid();

    public TimetableServiceTests()
    {
        _service = new TimetableService(TestEnvironment.Logger<TimetableService>(),
            new TimetableRepository(Env.Db, TestEnvironment.Logger<TimetableRepository>()), Env.Mapper, Env.AcademicStructure());
    }

    private TimetableRequest Period(Division division, Guid teacherId, int day = 1, int period = 1) => new()
    {
        ClassId = division.ClassId, DivisionId = division.Id, SubjectId = Maths.Id, TeacherId = teacherId,
        AcademicYearId = _yearId, DayOfWeek = day, PeriodNo = period,
    };

    [Fact]
    public async Task A_period_takes_the_branch_of_its_class()
    {
        var created = await _service.CreateAsync(Period(Class5A, Teacher.Id));

        Assert.Equal(Class5.BranchId, (await Env.Db.Timetables.FindAsync(created.Id))!.BranchId);
    }

    [Fact]
    public async Task A_class_slot_can_only_be_filled_once()
    {
        await _service.CreateAsync(Period(Class5A, Teacher.Id));

        await ErrorAssert.ConflictAsync(() => _service.CreateAsync(Period(Class5A, OtherTeacher.Id)));
    }

    [Fact]
    public async Task A_teacher_cannot_be_in_two_classes_at_the_same_time()
    {
        await _service.CreateAsync(Period(Class5A, Teacher.Id, day: 2, period: 3));

        await ErrorAssert.ConflictAsync(() => _service.CreateAsync(Period(Class6A, Teacher.Id, day: 2, period: 3)));
        await _service.CreateAsync(Period(Class6A, Teacher.Id, day: 2, period: 4));
        await _service.CreateAsync(Period(Class6A, OtherTeacher.Id, day: 2, period: 3));
    }

    [Fact]
    public async Task Moving_a_period_does_not_clash_with_itself()
    {
        var created = await _service.CreateAsync(Period(Class5A, Teacher.Id, day: 1, period: 1));

        var moved = await _service.UpdateAsync(created.Id, Period(Class5A, Teacher.Id, day: 1, period: 1));
        Assert.Equal(created.Id, moved.Id);
    }
}

public class HomeworkServiceTests : AcademicsTestBase
{
    private static readonly DateTimeOffset Today = new(2026, 11, 3, 9, 0, 0, TimeSpan.Zero);
    private readonly HomeworkService _service;

    public HomeworkServiceTests()
    {
        _service = new HomeworkService(TestEnvironment.Logger<HomeworkService>(),
            new HomeworkRepository(Env.Db, TestEnvironment.Logger<HomeworkRepository>()), Env.Mapper, Env.AcademicStructure(), new FixedClock(Today));
    }

    private HomeworkRequest Homework(DateOnly due) => new() { ClassId = Class5.Id, SubjectId = Maths.Id, Description = " Exercise 4.2 ", DueDate = due };

    [Fact]
    public async Task New_homework_cannot_be_due_in_the_past()
    {
        await ErrorAssert.BadRequestAsync(() => _service.CreateAsync(Homework(new DateOnly(2026, 10, 30))));

        var created = await _service.CreateAsync(Homework(new DateOnly(2026, 11, 5)));
        Assert.Equal("Exercise 4.2", created.Description);
    }

    [Fact]
    public async Task Existing_homework_keeps_its_past_due_date_when_edited()
    {
        var created = await _service.CreateAsync(Homework(new DateOnly(2026, 11, 3)));
        var later = new HomeworkService(TestEnvironment.Logger<HomeworkService>(),
            new HomeworkRepository(Env.Db, TestEnvironment.Logger<HomeworkRepository>()), Env.Mapper, Env.AcademicStructure(),
            new FixedClock(Today.AddDays(10)));

        var request = Homework(new DateOnly(2026, 11, 3));
        request.Description = "Exercise 4.2 and 4.3";
        var updated = await later.UpdateAsync(created.Id, request);

        Assert.Equal("Exercise 4.2 and 4.3", updated.Description);
    }

    [Fact]
    public async Task Homework_needs_an_existing_class()
    {
        var request = Homework(new DateOnly(2026, 11, 5));
        request.ClassId = Guid.NewGuid();

        await ErrorAssert.BadRequestAsync(() => _service.CreateAsync(request));
    }
}
