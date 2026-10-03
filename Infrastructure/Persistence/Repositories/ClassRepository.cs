using Application.Abstractions.Repositories;
using Domain.Entities.Postgres.Scaffolded;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories;

public class ClassRepository : Repository<Class>, IClassRepository
{
    public ClassRepository(Infrastructure.Persistence.Postgres.BonzerSchoolDbContext context, ILogger<ClassRepository> logger)
        : base(context, logger)
    {
    }
    // Add custom methods for SchoolClass if needed
}
