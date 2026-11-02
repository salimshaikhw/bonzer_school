using Application.ViewModels.AcademicYear;
using BonzerSchool.UnitTests.TestSupport;
using Domain.Entities.Organization;
using Infrastructure.Persistence.Repositories.Organization;
using Infrastructure.Services.Organization;

namespace BonzerSchool.UnitTests.Organization;

public class AcademicYearServiceTests : IDisposable
{
    private readonly TestEnvironment _env = new();
    private readonly AcademicYearService _service;

    public AcademicYearServiceTests()
    {
        var repository = new AcademicYearRepository(_env.Db, TestEnvironment.Logger<AcademicYearRepository>());
        _service = new AcademicYearService(repository, _env.Mapper, _env.Cache, TestEnvironment.Logger<AcademicYearService>());
    }

    private static AcademicYearRequest Year(string name, int startYear = 2026) => new()
    {
        YearName = name,
        StartDate = new DateOnly(startYear, 6, 1),
        EndDate = new DateOnly(startYear + 1, 4, 30),
    };

    [Fact]
    public async Task Create_saves_trimmed_name_and_defaults_to_active()
    {
        var created = await _service.CreateAsync(Year("  2026-27 "));

        var saved = await _env.Db.AcademicYears.FindAsync(created.Id);
        Assert.NotNull(saved);
        Assert.Equal("2026-27", saved!.YearName);
        Assert.True(saved.IsActive);
    }

    [Fact]
    public async Task Create_rejects_a_duplicate_name_ignoring_case()
    {
        await _service.CreateAsync(Year("2026-27"));

        await ErrorAssert.ConflictAsync(() => _service.CreateAsync(Year(" 2026-27 ".ToUpperInvariant())));
        Assert.Single(_env.Db.AcademicYears);
    }

    [Fact]
    public async Task Update_may_keep_its_own_name()
    {
        var created = await _service.CreateAsync(Year("2026-27"));

        var request = Year("2026-27");
        request.EndDate = new DateOnly(2027, 5, 31);
        var updated = await _service.UpdateAsync(created.Id, request);

        Assert.Equal(new DateOnly(2027, 5, 31), updated.EndDate);
    }

    [Fact]
    public async Task Update_cannot_take_the_name_of_another_year()
    {
        await _service.CreateAsync(Year("2025-26", 2025));
        var current = await _service.CreateAsync(Year("2026-27"));

        await ErrorAssert.ConflictAsync(() => _service.UpdateAsync(current.Id, Year("2025-26", 2025)));
    }

    [Fact]
    public async Task Missing_year_is_not_found()
    {
        await ErrorAssert.NotFoundAsync(() => _service.GetByIdAsync(Guid.NewGuid()));
        await ErrorAssert.NotFoundAsync(() => _service.UpdateAsync(Guid.NewGuid(), Year("2030-31")));
        await ErrorAssert.NotFoundAsync(() => _service.DeleteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task List_is_cached_and_refreshed_after_a_change()
    {
        await _service.CreateAsync(Year("2025-26", 2025));
        Assert.Single(await _service.GetAllAsync());

        // A row written behind the service's back is not visible while the list is cached...
        _env.Db.AcademicYears.Add(new AcademicYear { Id = Guid.NewGuid(), YearName = "2024-25" });
        await _env.Db.SaveChangesAsync();
        Assert.Single(await _service.GetAllAsync());

        // ...but any change through the service invalidates the cache.
        await _service.CreateAsync(Year("2026-27"));
        Assert.Equal(3, (await _service.GetAllAsync()).Count());
    }

    [Fact]
    public async Task Delete_removes_the_year()
    {
        var created = await _service.CreateAsync(Year("2026-27"));

        Assert.True(await _service.DeleteAsync(created.Id));
        Assert.Empty(_env.Db.AcademicYears);
    }

    public void Dispose() => _env.Dispose();
}
