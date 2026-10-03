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
    public class FeeStructureRepository : Repository<FeeStructure>,IFeeStructureRepository
    {
        public FeeStructureRepository(BonzerSchoolDbContext context, ILogger<FeeStructureRepository> logger) : base(context,logger)
        {

        }
    }
}
