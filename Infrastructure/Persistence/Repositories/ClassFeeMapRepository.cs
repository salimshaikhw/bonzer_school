using Application.Abstractions.Repositories;
using Domain.Entities.Postgres.Scaffolded;
using Infrastructure.Persistence.Postgres;
using Microsoft.Extensions.Logging;
using NPOI.OpenXml4Net.OPC.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories
{
    public class ClassFeeMapRepository : Repository<ClassFeeMap>, IClassFeeMapRepository
    {
        public ClassFeeMapRepository(BonzerSchoolDbContext context, ILogger<ClassFeeMapRepository> logger): base(context, logger)
        { 
        
        }

    }
}


