using Application.Abstractions.Repositories;
using Infrastructure.Persistence.Postgres;
using Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories.Academics
{
    public class ClassTeacherMapRepository : Repository<ClassTeacherMap>, IClassTeacherMapRepository
    {
        public ClassTeacherMapRepository(BonzerSchoolDbContext context, ILogger<ClassTeacherMapRepository> logger) : base(context,logger)
        {

        }
    }
}


