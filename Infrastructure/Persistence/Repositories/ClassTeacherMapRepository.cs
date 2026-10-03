using Application.Abstractions.Repositories;
using Domain.Entities.Postgres.Scaffolded;
using Infrastructure.Persistence.Postgres;
using Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories
{
    public class ClassTeacherMapRepository : Repository<ClassTeacherMap>, IClassTeacherMapRepository
    {
        public ClassTeacherMapRepository(BonzerSchoolDbContext context, ILogger<ClassTeacherMapRepository> logger) : base(context,logger)
        {

        }
    }
}


