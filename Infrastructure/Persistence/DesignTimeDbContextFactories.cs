using Infrastructure.Persistence.Catalog;
using Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Persistence;

// Used only by "dotnet ef migrations add". No database is contacted; the app applies migrations itself
// (catalog at startup, tenants at startup and on provisioning).
//   dotnet ef migrations add <Name> --context CatalogDbContext     --project Infrastructure --output-dir Persistence/Migrations/Catalog
//   dotnet ef migrations add <Name> --context BonzerSchoolDbContext --project Infrastructure --output-dir Persistence/Migrations/Tenant

internal sealed class CatalogDbContextDesignTimeFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<CatalogDbContext>().UseNpgsql("Host=localhost;Database=bonzer_catalog").Options);
}

internal sealed class TenantDbContextDesignTimeFactory : IDesignTimeDbContextFactory<BonzerSchoolDbContext>
{
    public BonzerSchoolDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<BonzerSchoolDbContext>().UseNpgsql("Host=localhost;Database=bonzer_school_design").Options);
}
