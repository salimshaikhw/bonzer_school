using Application.Abstractions.Repositories;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories.Fees
{
    public class PaymentRepository : Repository<Payment>, IPaymentRepository
    {
        public PaymentRepository(Infrastructure.Persistence.Postgres.BonzerSchoolDbContext context, ILogger<PaymentRepository> logger)
            : base(context, logger) { }
        // Add custom methods for PaymentRepository here if needed
    }
}
