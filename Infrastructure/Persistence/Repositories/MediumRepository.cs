using Application.Abstractions.Repositories;
using Domain.Entities.Postgres;
using Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories
{
    public class MediumRepository : Repository<Medium>, IMediumRepository
    {
        public MediumRepository(BonzerSchoolDbContext context, ILogger<MediumRepository> logger)
            : base(context, logger)
        {
        }
        // Add unique methods for Medium if needed
    }
}