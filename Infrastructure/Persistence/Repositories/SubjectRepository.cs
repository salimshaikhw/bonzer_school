using Application.Abstractions.Repositories;
using Domain.Entities.Postgres.Scaffolded;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories;

public class SubjectRepository : Repository<Subject>, ISubjectRepository
{
    public SubjectRepository(Infrastructure.Persistence.Postgres.BonzerSchoolDbContext context, ILogger<SubjectRepository> logger)
        : base(context, logger)
    {
    }
    // Add custom methods for subject if needed
}
