using API.ExceptionHandling;
using Application.Abstractions.Tenancy;
using Application.Common.Helpers.MemoryCache;
using AutoMapper;
using Infrastructure.Persistence.Catalog;
using Infrastructure.Persistence.Postgres;
using Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BonzerSchool.UnitTests.TestSupport;

/// <summary>
/// Everything a service test needs, wired like the real app but in memory: a tenant database and a catalog
/// database (EF Core InMemory, fresh per test), the real AutoMapper profiles, the real tenant-scoped cache
/// and a selected tenant. Create one per test.
/// </summary>
public sealed class TestEnvironment : IDisposable
{
    private static readonly IMapper SharedMapper =
        new MapperConfiguration(cfg => cfg.AddMaps(typeof(Infrastructure.Mapping.BranchProfile).Assembly)).CreateMapper();

    public TestEnvironment()
    {
        var name = Guid.NewGuid().ToString();
        Db = new BonzerSchoolDbContext(new DbContextOptionsBuilder<BonzerSchoolDbContext>().UseInMemoryDatabase("tenant-" + name).Options);
        Catalog = new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>().UseInMemoryDatabase("catalog-" + name).Options);

        TenantContext = new TenantContext();
        TenantContext.SetTenant(Tenant);
        Cache = new MemoryCacheHelper(new MemoryCache(new MemoryCacheOptions()), TenantContext);
    }

    /// <summary>The tenant of every test; its school group (catalog organizations row) has the same id.</summary>
    public TenantInfo Tenant { get; } = new(Guid.NewGuid(), "test-school", "Test School Group", "bonzer_school_test", true);

    public BonzerSchoolDbContext Db { get; }
    public CatalogDbContext Catalog { get; }
    public TenantContext TenantContext { get; }
    public IMemoryCacheHelper Cache { get; }
    public IMapper Mapper => SharedMapper;

    public static ILogger<T> Logger<T>() => NullLogger<T>.Instance;

    public void Dispose()
    {
        Db.Dispose();
        Catalog.Dispose();
    }
}

/// <summary>Assertions on the HTTP status an exception will be answered with (via the real ExceptionClassifier).</summary>
public static class ErrorAssert
{
    public static async Task<Exception> StatusAsync(int expectedStatus, Func<Task> action)
    {
        var exception = await Record.ExceptionAsync(action);
        Assert.NotNull(exception);
        var classification = ExceptionClassifier.Classify(exception!, requestAborted: false);
        Assert.True(expectedStatus == classification.StatusCode,
            $"Expected HTTP {expectedStatus} but the exception maps to {classification.StatusCode} ({classification.ErrorCode}): {exception!.Message}");
        return exception!;
    }

    public static Task<Exception> BadRequestAsync(Func<Task> action) => StatusAsync(400, action);
    public static Task<Exception> NotFoundAsync(Func<Task> action) => StatusAsync(404, action);
    public static Task<Exception> ConflictAsync(Func<Task> action) => StatusAsync(409, action);
}
