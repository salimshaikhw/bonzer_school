using Application.Abstractions.Repositories;
using Infrastructure;
using Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories.Communication
{
    public class NotificationRepository : Repository<Notification>, INotificationRepository
    {
        public NotificationRepository(BonzerSchoolDbContext context, ILogger<NotificationRepository> logger)
            : base(context, logger)
        {
        }

        // Add only unique methods for StudentRepository here
    }
}