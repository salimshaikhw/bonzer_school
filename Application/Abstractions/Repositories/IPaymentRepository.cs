using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Abstractions.Repositories
{
    public interface IPaymentRepository : IRepository<Domain.Entities.Postgres.Scaffolded.Payment>
    {
        // Add custom methods for PaymentRepository here if needed
    }
}
