using Application.Abstractions.Repositories;
using Domain.Entities.Postgres.Scaffolded;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories
{
    public class PaymentRepository : Repository<Payment>, IPaymentRepository
    {
        public PaymentRepository(Infrastructure.Persistence.Postgres.BonzerSchoolDbContext context, ILogger<PaymentRepository> logger)
            : base(context, logger) { }
        // Add custom methods for PaymentRepository here if needed
    }
}
