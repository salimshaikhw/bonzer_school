using Application.Abstractions.Repositories;
using Domain.Entities.Postgres.Scaffolded;
using Infrastructure.Persistence.Postgres;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories;

public class AttendanceCorrectionRequestRepository : Repository<AttendanceCorrectionRequest>, IAttendanceCorrectionRequestRepository
{
    public AttendanceCorrectionRequestRepository(BonzerSchoolDbContext context, ILogger<AttendanceCorrectionRequestRepository> logger)
        : base(context, logger)
    {
    }
}
