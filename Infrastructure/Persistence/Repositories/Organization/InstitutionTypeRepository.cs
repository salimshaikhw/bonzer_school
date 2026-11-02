using Application.Abstractions.Repositories;
using Infrastructure.Persistence.Catalog;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories.Organization
{
    /// <summary>Shared lookup: reads and writes the catalog database.</summary>
    public class InstitutionTypeRepository : Repository<InstitutionType>, IInstitutionTypeRepository
    {
        public InstitutionTypeRepository(CatalogDbContext context,ILogger<InstitutionTypeRepository> logger) : base(context, logger)
        {

        }
    }
}
