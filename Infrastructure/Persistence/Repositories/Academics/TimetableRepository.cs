using Application.Abstractions.Repositories;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories.Academics;

public class TimetableRepository : Repository<Timetable>, ITimetableRepository
{
    public TimetableRepository(Infrastructure.Persistence.Postgres.BonzerSchoolDbContext context, ILogger<TimetableRepository> logger)
        : base(context, logger)
    {
    }
}
