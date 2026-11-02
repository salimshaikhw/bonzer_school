using Application.Abstractions.Repositories;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories.Organization;

public class SchoolGroupRepository : Repository<SchoolGroup>, ISchoolGroupRepository
{
    public SchoolGroupRepository(Infrastructure.Persistence.Postgres.BonzerSchoolDbContext context, ILogger<SchoolGroupRepository> logger)
        : base(context, logger)
    {
    }
    // Add custom methods for school_group if needed
}
