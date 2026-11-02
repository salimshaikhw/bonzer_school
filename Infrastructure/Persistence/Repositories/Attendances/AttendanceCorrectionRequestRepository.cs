using Application.Abstractions.Repositories;
using Infrastructure.Persistence.Postgres;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories.Attendances;

public class AttendanceCorrectionRequestRepository : Repository<AttendanceCorrectionRequest>, IAttendanceCorrectionRequestRepository
{
    public AttendanceCorrectionRequestRepository(BonzerSchoolDbContext context, ILogger<AttendanceCorrectionRequestRepository> logger)
        : base(context, logger)
    {
    }
}
