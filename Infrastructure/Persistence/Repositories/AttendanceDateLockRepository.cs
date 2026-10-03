using Application.Abstractions.Repositories;
using Domain.Entities.Postgres.Scaffolded;
using Infrastructure.Persistence.Postgres;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories;

public class AttendanceDateLockRepository : Repository<AttendanceDateLock>, IAttendanceDateLockRepository
{
    public AttendanceDateLockRepository(BonzerSchoolDbContext context, ILogger<AttendanceDateLockRepository> logger)
        : base(context, logger)
    {
    }
}
