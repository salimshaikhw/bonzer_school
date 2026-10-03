using Application.Abstractions.Repositories;
using Domain.Entities.Postgres.Scaffolded;
using Infrastructure.Persistence.Postgres;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories;

public class AttendanceAcademicYearLockRepository : Repository<AttendanceAcademicYearLock>, IAttendanceAcademicYearLockRepository
{
    public AttendanceAcademicYearLockRepository(BonzerSchoolDbContext context, ILogger<AttendanceAcademicYearLockRepository> logger)
        : base(context, logger)
    {
    }
}
