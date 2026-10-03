using Application.Abstractions.Repositories;
using Domain.Entities.Postgres.Scaffolded;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories;

public class TeacherSubjectMapRepository : Repository<TeacherSubjectMap>, ITeacherSubjectMapRepository
{
    public TeacherSubjectMapRepository(Infrastructure.Persistence.Postgres.BonzerSchoolDbContext context, ILogger<TeacherSubjectMapRepository> logger)
        : base(context, logger)
    {
    }
}
