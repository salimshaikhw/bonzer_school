using Domain.Entities.Postgres.Scaffolded;

namespace Application.Abstractions.Repositories;

public interface IUserRepository : IRepository<User>
{
    // Add custom methods for user if needed
}
