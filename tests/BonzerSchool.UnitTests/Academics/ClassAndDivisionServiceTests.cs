using Application.ViewModels.Class;
using Application.ViewModels.Division;
using BonzerSchool.UnitTests.TestSupport;
using Infrastructure.Persistence.Repositories.Academics;
using Infrastructure.Services.Academics;

namespace BonzerSchool.UnitTests.Academics;

public class ClassServiceTests : IDisposable
{
    private readonly TestEnvironment _env = new();
    private readonly ClassService _service;
    private readonly Guid _branchId;

    public ClassServiceTests()
    {
        _branchId = _env.AddBranch().Id;
        _service = new ClassService(new ClassRepository(_env.Db, TestEnvironment.Logger<ClassRepository>()), _env.Mapper, _env.Cache,
            TestEnvironment.Logger<ClassService>(), _env.AcademicStructure(), new DivisionRepository(_env.Db, TestEnvironment.Logger<DivisionRepository>()));
    }

    [Fact]
    public async Task Create_requires_an_existing_branch()
    {
        await ErrorAssert.BadRequestAsync(() => _service.CreateAsync(new ClassRequest { BranchId = Guid.NewGuid(), Name = "Class 1" }));
    }

    [Fact]
    public async Task Class_names_are_unique_per_branch_only()
    {
        var otherBranch = _env.AddBranch("City Campus").Id;
        await _service.CreateAsync(new ClassRequest { BranchId = _branchId, Name = "Class 1" });

        await ErrorAssert.ConflictAsync(() => _service.CreateAsync(new ClassRequest { BranchId = _branchId, Name = " class 1" }));
        var sameNameElsewhere = await _service.CreateAsync(new ClassRequest { BranchId = otherBranch, Name = "Class 1" });
        Assert.NotEqual(Guid.Empty, sameNameElsewhere.Id);
    }

    [Fact]
    public async Task A_class_with_divisions_cannot_be_deleted()
    {
        var cls = _env.AddClass(_branchId);
        _env.AddDivision(cls);

        await ErrorAssert.ConflictAsync(() => _service.DeleteAsync(cls.Id));
    }

    [Fact]
    public async Task Get_by_id_reads_without_blocking_and_misses_are_404()
    {
        var cls = _env.AddClass(_branchId);

        Assert.Equal("Class 5", (await _service.GetByIdAsync(cls.Id)).Name);
        await ErrorAssert.NotFoundAsync(() => _service.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Classes_can_be_listed_per_branch()
    {
        var otherBranch = _env.AddBranch("City Campus").Id;
        _env.AddClass(_branchId, "Class 1");
        _env.AddClass(_branchId, "Class 2");
        _env.AddClass(otherBranch, "Class 1");

        Assert.Equal(2, (await _service.GetByBranchIdAsync(_branchId)).Count());
    }

    public void Dispose() => _env.Dispose();
}

public class DivisionServiceTests : IDisposable
{
    private readonly TestEnvironment _env = new();
    private readonly DivisionService _service;
    private readonly Domain.Entities.Academics.Class _class;

    public DivisionServiceTests()
    {
        _class = _env.AddClass(_env.AddBranch().Id);
        _service = new DivisionService(new DivisionRepository(_env.Db, TestEnvironment.Logger<DivisionRepository>()),
            TestEnvironment.Logger<DivisionService>(), _env.Mapper, _env.Cache, _env.AcademicStructure());
    }

    [Fact]
    public async Task The_branch_is_taken_from_the_class()
    {
        var created = await _service.CreateAsync(new DivisionRequest { ClassId = _class.Id, Name = "A" });

        Assert.Equal(_class.BranchId, (await _env.Db.Divisions.FindAsync(created.Id))!.BranchId);
    }

    [Fact]
    public async Task A_different_branch_than_the_class_is_rejected()
    {
        await ErrorAssert.BadRequestAsync(() =>
            _service.CreateAsync(new DivisionRequest { ClassId = _class.Id, BranchId = Guid.NewGuid(), Name = "A" }));
    }

    [Fact]
    public async Task Unknown_class_is_rejected()
    {
        await ErrorAssert.BadRequestAsync(() => _service.CreateAsync(new DivisionRequest { ClassId = Guid.NewGuid(), Name = "A" }));
        await ErrorAssert.BadRequestAsync(() => _service.CreateAsync(new DivisionRequest { Name = "A" }));
    }

    [Fact]
    public async Task Division_names_are_unique_within_a_class()
    {
        var otherClass = _env.AddClass(_class.BranchId!.Value, "Class 6");
        await _service.CreateAsync(new DivisionRequest { ClassId = _class.Id, Name = "A" });

        await ErrorAssert.ConflictAsync(() => _service.CreateAsync(new DivisionRequest { ClassId = _class.Id, Name = "a" }));
        await _service.CreateAsync(new DivisionRequest { ClassId = otherClass.Id, Name = "A" });
    }

    public void Dispose() => _env.Dispose();
}
