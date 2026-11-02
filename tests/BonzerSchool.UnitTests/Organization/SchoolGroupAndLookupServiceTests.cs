using Application.ViewModels.InstitutionType;
using Application.ViewModels.Medium;
using Application.ViewModels.SchoolGroup;
using BonzerSchool.UnitTests.TestSupport;
using Domain.Entities.Organization;
using Infrastructure.Persistence.Repositories.Organization;
using Infrastructure.Services.Organization;

namespace BonzerSchool.UnitTests.Organization;

public class SchoolGroupServiceTests : IDisposable
{
    private readonly TestEnvironment _env = new();
    private readonly SchoolGroupService _service;

    public SchoolGroupServiceTests()
    {
        _env.Db.SchoolGroups.Add(new SchoolGroup { Id = _env.Tenant.Id, Name = "Test School Group" });
        _env.Db.SaveChanges();
        var repository = new SchoolGroupRepository(_env.Db, TestEnvironment.Logger<SchoolGroupRepository>());
        _service = new SchoolGroupService(repository, _env.Mapper, _env.Cache, TestEnvironment.Logger<SchoolGroupService>(), _env.TenantContext);
    }

    [Fact]
    public async Task A_second_school_group_cannot_be_created()
    {
        await ErrorAssert.ConflictAsync(() => _service.CreateAsync(new CreateSchoolGroupInput { Name = "Other", Code = "X" }));
        Assert.Single(_env.Db.SchoolGroups);
    }

    [Fact]
    public async Task The_school_group_cannot_be_deleted()
    {
        await ErrorAssert.ConflictAsync(() => _service.DeleteAsync(_env.Tenant.Id));
        Assert.Single(_env.Db.SchoolGroups);
    }

    [Fact]
    public async Task The_own_group_can_be_read_and_renamed()
    {
        var updated = await _service.UpdateAsync(_env.Tenant.Id, new UpdateSchoolGroupInput { Name = "Renamed Trust", Code = "RT" });

        Assert.Equal("Renamed Trust", updated.Name);
        Assert.Equal("Renamed Trust", (await _service.GetByIdAsync(_env.Tenant.Id)).Name);
    }

    [Fact]
    public async Task Other_group_ids_are_not_found()
    {
        await ErrorAssert.NotFoundAsync(() => _service.GetByIdAsync(Guid.NewGuid()));
    }

    public void Dispose() => _env.Dispose();
}

public class LookupServiceTests : IDisposable
{
    private readonly TestEnvironment _env = new();
    private readonly MediumService _mediums;
    private readonly InstitutionTypeService _institutionTypes;

    public LookupServiceTests()
    {
        _mediums = new MediumService(new MediumRepository(_env.Catalog, TestEnvironment.Logger<MediumRepository>()),
            TestEnvironment.Logger<MediumService>(), _env.Mapper);
        _institutionTypes = new InstitutionTypeService(new InstitutionTypeRepository(_env.Catalog, TestEnvironment.Logger<InstitutionTypeRepository>()),
            TestEnvironment.Logger<InstitutionTypeService>(), _env.Mapper);
    }

    [Fact]
    public async Task Medium_names_are_unique()
    {
        var english = await _mediums.CreateAsync(new MediumRequest { Name = " English " });
        Assert.Equal("English", english.Name);

        await ErrorAssert.ConflictAsync(() => _mediums.CreateAsync(new MediumRequest { Name = "ENGLISH" }));
    }

    [Fact]
    public async Task Institution_type_names_are_unique_but_can_be_updated_in_place()
    {
        var created = await _institutionTypes.CreateAsync(new InstitutionTypeRequest { Name = "Coaching" });
        await _institutionTypes.CreateAsync(new InstitutionTypeRequest { Name = "University" });

        await _institutionTypes.UpdateAsync(created.Id, new InstitutionTypeRequest { Name = "coaching" });
        await ErrorAssert.ConflictAsync(() => _institutionTypes.UpdateAsync(created.Id, new InstitutionTypeRequest { Name = "University" }));
    }

    public void Dispose() => _env.Dispose();
}
