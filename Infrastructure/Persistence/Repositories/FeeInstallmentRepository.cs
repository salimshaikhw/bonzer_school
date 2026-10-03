using Application.Abstractions.Repositories;
using Infrastructure.Persistence.Postgres;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories
{
    public class FeeInstallmentRepository : Repository<FeeInstallment>, IFeeInstallmentRepository
    {
        public FeeInstallmentRepository(BonzerSchoolDbContext context , ILogger<FeeInstallmentRepository> logger) : base(context,logger)
        {

        }
    }
}
