using Application.Abstractions.Repositories;
using Domain.Entities.Postgres.Scaffolded;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories;

public class TimetableRepository : Repository<Timetable>, ITimetableRepository
{
    public TimetableRepository(Infrastructure.Persistence.Postgres.BonzerSchoolDbContext context, ILogger<TimetableRepository> logger)
        : base(context, logger)
    {
    }
}
