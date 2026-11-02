using Application.Abstractions.Repositories;
using Infrastructure.Persistence.Postgres;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories.Certificates;

public class BonafideCertificateRepository : Repository<BonafideCertificate>, IBonafideCertificateRepository
{
    public BonafideCertificateRepository(BonzerSchoolDbContext context, ILogger<BonafideCertificateRepository> logger)
        : base(context, logger) { }
}
