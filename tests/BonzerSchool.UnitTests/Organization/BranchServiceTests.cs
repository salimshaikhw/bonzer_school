using Application.ViewModels.Branch;
using BonzerSchool.UnitTests.TestSupport;
using Infrastructure.Persistence.Repositories.Organization;
using Infrastructure.Services.Organization;

namespace BonzerSchool.UnitTests.Organization;

public class BranchServiceTests : IDisposable
{
    private readonly TestEnvironment _env = new();
    private readonly BranchService _service;

    public BranchServiceTests()
    {
        var repository = new BranchRepository(_env.Db, TestEnvironment.Logger<BranchRepository>());
        _service = new BranchService(repository, _env.Mapper, _env.Cache, TestEnvironment.Logger<BranchService>(), _env.TenantContext);
    }

    private static BranchRequest Branch(string name, string? code = null) => new() { Name = name, Schooluid = code, City = "Pune" };

    [Fact]
    public async Task Create_puts_the_branch_in_the_tenants_school_group()
    {
        var created = await _service.CreateAsync(Branch("Main Campus"));

        var saved = await _env.Db.Branches.FindAsync(created.Id);
        Assert.Equal(_env.Tenant.Id, saved!.GroupId);
    }

    [Fact]
    public async Task Create_rejects_another_school_group()
    {
        var request = Branch("Main Campus");
        request.GroupId = Guid.NewGuid();

        await ErrorAssert.BadRequestAsync(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task Branch_names_are_unique_ignoring_case_and_spaces()
    {
        await _service.CreateAsync(Branch("Main Campus"));

        await ErrorAssert.ConflictAsync(() => _service.CreateAsync(Branch("  main campus ")));
    }

    [Fact]
    public async Task School_codes_are_unique_but_optional()
    {
        await _service.CreateAsync(Branch("Main Campus", "27250100101"));
        await _service.CreateAsync(Branch("City Campus"));
        await _service.CreateAsync(Branch("Hill Campus", "   ")); // blank code is stored as no code

        await ErrorAssert.ConflictAsync(() => _service.CreateAsync(Branch("West Campus", "27250100101")));
        Assert.Equal(2, _env.Db.Branches.Count(b => b.SchoolUid == null));
    }

    [Fact]
    public async Task Update_can_rename_but_not_to_an_existing_name()
    {
        await _service.CreateAsync(Branch("Main Campus"));
        var city = await _service.CreateAsync(Branch("City Campus"));

        var renamed = await _service.UpdateAsync(city.Id, Branch("City Campus East"));
        Assert.Equal("City Campus East", renamed.Name);

        await ErrorAssert.ConflictAsync(() => _service.UpdateAsync(city.Id, Branch("Main Campus")));
    }

    public void Dispose() => _env.Dispose();
}
