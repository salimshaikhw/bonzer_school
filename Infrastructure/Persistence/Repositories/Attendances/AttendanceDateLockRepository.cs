using Application.Abstractions.Repositories;
using Infrastructure.Persistence.Postgres;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories.Attendances;

public class AttendanceDateLockRepository : Repository<AttendanceDateLock>, IAttendanceDateLockRepository
{
    public AttendanceDateLockRepository(BonzerSchoolDbContext context, ILogger<AttendanceDateLockRepository> logger)
        : base(context, logger)
    {
    }
}
