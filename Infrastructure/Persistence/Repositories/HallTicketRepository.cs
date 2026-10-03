using Application.Abstractions.Repositories;
using Infrastructure.Persistence.Postgres;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories;

public class HallTicketRepository : Repository<HallTicket>, IHallTicketRepository
{
    public HallTicketRepository(BonzerSchoolDbContext context, ILogger<HallTicketRepository> logger)
        : base(context, logger) { }
}
