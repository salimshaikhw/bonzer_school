using Application.Abstractions.Repositories;
using Infrastructure.Persistence.Postgres;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories.Attendances;

public class AttendanceAcademicYearLockRepository : Repository<AttendanceAcademicYearLock>, IAttendanceAcademicYearLockRepository
{
    public AttendanceAcademicYearLockRepository(BonzerSchoolDbContext context, ILogger<AttendanceAcademicYearLockRepository> logger)
        : base(context, logger)
    {
    }
}
