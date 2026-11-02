using Application.Abstractions.Repositories;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories.Academics;

public class ClassRepository : Repository<Class>, IClassRepository
{
    public ClassRepository(Infrastructure.Persistence.Postgres.BonzerSchoolDbContext context, ILogger<ClassRepository> logger)
        : base(context, logger)
    {
    }
    // Add custom methods for SchoolClass if needed
}
