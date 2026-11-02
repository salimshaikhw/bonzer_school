using Application.Abstractions.Repositories;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories.Academics;

public class DivisionRepository : Repository<Division>, IDivisionRepository
{
    public DivisionRepository(Infrastructure.Persistence.Postgres.BonzerSchoolDbContext context, ILogger<DivisionRepository> logger)
        : base(context, logger)
    {
    }
    // Add custom methods for division if needed
}
