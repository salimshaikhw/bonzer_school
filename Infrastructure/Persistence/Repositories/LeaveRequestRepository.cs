using Application.Abstractions.Repositories;
using Domain.Entities.Postgres.Scaffolded;
using Infrastructure.Persistence.Postgres;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories;

public class LeaveRequestRepository : Repository<LeaveRequest>, ILeaveRequestRepository
{
    public LeaveRequestRepository(BonzerSchoolDbContext context, ILogger<LeaveRequestRepository> logger)
        : base(context, logger)
    {
    }
}
