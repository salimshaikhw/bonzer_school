using Application.ViewModels.InstitutionType;
using Application.ViewModels.Medium;
using Application.ViewModels.SchoolGroup;
using BonzerSchool.UnitTests.TestSupport;
using Application.Abstractions.Tenancy;
using Domain.Entities.Tenancy;
using Infrastructure.Persistence.Repositories.Organization;
using Infrastructure.Tenancy;
using NSubstitute;
using Infrastructure.Services.Organization;

namespace BonzerSchool.UnitTests.Organization;

public class SchoolGroupServiceTests : IDisposable
{
    private readonly TestEnvironment _env = new();
    private readonly SchoolGroupService _service;

    public SchoolGroupServiceTests()
    {
        // The school group lives in the catalog database (table "organizations"); the tenant id is its id.
        _env.Catalog.SchoolGroups.Add(new SchoolGroup
        {
            Id = _env.Tenant.Id, Identifier = _env.Tenant.Identifier, Name = "Test School Group", DatabaseName = _env.Tenant.DatabaseName,
        });
        _env.Catalog.SchoolGroups.Add(new SchoolGroup
        {
            Id = Guid.NewGuid(), Identifier = "other-trust", Name = "Other Trust", DatabaseName = "bonzer_school_other_trust",
        });
        _env.Catalog.SaveChanges();
        // Provisioning (database, login) is not exercised here, so those dependencies are not needed.
        _service = new SchoolGroupService(_env.Catalog, null!, null!, null!, Substitute.For<ITenantStore>(),
            _env.TenantContext, TestEnvironment.Logger<SchoolGroupService>());
    }

    [Fact]
    public async Task A_tenant_user_only_sees_their_own_group()
    {
        var groups = (await _service.GetAllAsync()).ToList();

        Assert.Single(groups);
        Assert.Equal(_env.Tenant.Id, groups[0].Id);
    }

    [Fact]
    public async Task The_own_group_can_be_read_and_renamed()
    {
        var updated = await _service.UpdateAsync(_env.Tenant.Id, new UpdateSchoolGroupInput { Name = "Renamed Trust", Code = "RT" });

        Assert.Equal("Renamed Trust", updated.Name);
        Assert.Equal("Renamed Trust", (await _service.GetByIdAsync(_env.Tenant.Id)).Name);
    }

    [Fact]
    public async Task Other_groups_are_not_found_for_a_tenant_user()
    {
        var other = _env.Catalog.SchoolGroups.Single(g => g.Identifier == "other-trust");

        await ErrorAssert.NotFoundAsync(() => _service.GetByIdAsync(other.Id));
        await ErrorAssert.NotFoundAsync(() => _service.UpdateAsync(other.Id, new UpdateSchoolGroupInput { Name = "Hijacked" }));
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
