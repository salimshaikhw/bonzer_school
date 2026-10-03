using Application.Abstractions.Repositories;
using Infrastructure;
using Infrastructure.Persistence.Postgres;
using Microsoft.Extensions.Logging;


namespace Infrastructure.Persistence.Repositories;

public class AcademicYearRepository : Repository<AcademicYear>, IAcademicYearRepository
{
    public AcademicYearRepository(BonzerSchoolDbContext context, ILogger<AcademicYearRepository> logger)
        : base(context, logger)
    {
    }
    // Add custom methods for academic_year if needed
}
