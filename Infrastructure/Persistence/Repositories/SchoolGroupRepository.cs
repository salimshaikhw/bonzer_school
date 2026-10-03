using Application.Abstractions.Repositories;
using Domain.Entities.Postgres.Scaffolded;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories;

public class SchoolGroupRepository : Repository<SchoolGroup>, ISchoolGroupRepository
{
    public SchoolGroupRepository(Infrastructure.Persistence.Postgres.BonzerSchoolDbContext context, ILogger<SchoolGroupRepository> logger)
        : base(context, logger)
    {
    }
    // Add custom methods for school_group if needed
}
