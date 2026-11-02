using Application.Abstractions.Repositories;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories.Academics;

public class SubjectRepository : Repository<Subject>, ISubjectRepository
{
    public SubjectRepository(Infrastructure.Persistence.Postgres.BonzerSchoolDbContext context, ILogger<SubjectRepository> logger)
        : base(context, logger)
    {
    }
    // Add custom methods for subject if needed
}
