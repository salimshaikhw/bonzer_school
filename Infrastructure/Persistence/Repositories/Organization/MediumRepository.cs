using Application.Abstractions.Repositories;
using Infrastructure.Persistence.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories.Organization
{
    /// <summary>Shared lookup: reads and writes the catalog database.</summary>
    public class MediumRepository : Repository<Medium>, IMediumRepository
    {
        public MediumRepository(CatalogDbContext context, ILogger<MediumRepository> logger)
            : base(context, logger)
        {
        }
        // Add unique methods for Medium if needed
    }
}