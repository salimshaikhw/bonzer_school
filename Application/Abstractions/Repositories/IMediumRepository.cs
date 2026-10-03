using Domain.Entities.Postgres;
using Infrastructure;

namespace Application.Abstractions.Repositories
{
    public interface IMediumRepository : IRepository<Medium>
    {
        // Add unique methods for Medium if needed
    }
}