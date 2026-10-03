using Application.Abstractions.Repositories;
using Domain.Entities.Postgres.Scaffolded;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(Infrastructure.Persistence.Postgres.BonzerSchoolDbContext context, ILogger<UserRepository> logger)
        : base(context, logger)
    {
    }
    // Add custom methods for user if needed
}
