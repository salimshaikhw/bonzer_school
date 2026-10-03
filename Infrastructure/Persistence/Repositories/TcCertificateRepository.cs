using Application.Abstractions.Repositories;
using Infrastructure.Persistence.Postgres;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories;

public class TcCertificateRepository : Repository<TcCertificate>, ITcCertificateRepository
{
    public TcCertificateRepository(BonzerSchoolDbContext context, ILogger<TcCertificateRepository> logger)
        : base(context, logger) { }
}
