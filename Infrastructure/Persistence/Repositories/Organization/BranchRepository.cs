using Application.Abstractions.Repositories;
using Microsoft.Extensions.Logging;
using Infrastructure.Persistence.Postgres;

namespace Infrastructure.Persistence.Repositories.Organization;

public class BranchRepository : Repository<Branch>, IBranchRepository
{
    public BranchRepository(BonzerSchoolDbContext context, ILogger<BranchRepository> logger)
        : base(context, logger)
    {
    }
    // Add custom methods for branch if needed
}
