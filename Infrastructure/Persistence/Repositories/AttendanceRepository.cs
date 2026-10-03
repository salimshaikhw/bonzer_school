using Application.Abstractions.Repositories;
using Infrastructure;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories;

public class AttendanceRepository : Repository<Attendance>, IAttendanceRepository
{
    public AttendanceRepository(Infrastructure.Persistence.Postgres.BonzerSchoolDbContext context, ILogger<AttendanceRepository> logger)
        : base(context, logger)
    {
    }
    // Add custom methods for attendance if needed
}
