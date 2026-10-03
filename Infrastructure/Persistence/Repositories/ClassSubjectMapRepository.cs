using Application.Abstractions.Repositories;
using Domain.Entities.Postgres.Scaffolded;
using Infrastructure.Persistence.Postgres;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Infrastructure.Persistence.Repositories
{
    public class ClassSubjectMapRepository : Repository<ClassSubjectMap>, IClassSubjectMapRepository
    {
        public ClassSubjectMapRepository (BonzerSchoolDbContext context, ILogger<ClassSubjectMapRepository> logger) : base(context,logger)
        {

        }
    }
}




