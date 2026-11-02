using Application.Abstractions.Repositories;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories.Academics;

public class TeacherSubjectMapRepository : Repository<TeacherSubjectMap>, ITeacherSubjectMapRepository
{
    public TeacherSubjectMapRepository(Infrastructure.Persistence.Postgres.BonzerSchoolDbContext context, ILogger<TeacherSubjectMapRepository> logger)
        : base(context, logger)
    {
    }
}
