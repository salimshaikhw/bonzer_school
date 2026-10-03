using Application.Abstractions.Repositories;
using Infrastructure.Persistence.Postgres;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories
{
    public class StudentSubjectRepository : Repository<StudentSubject>, IStudentSubjectRepository
    {
        public StudentSubjectRepository(BonzerSchoolDbContext context, ILogger<StudentSubjectRepository> logger) : base(context,logger)
        {

        }
    }
}
