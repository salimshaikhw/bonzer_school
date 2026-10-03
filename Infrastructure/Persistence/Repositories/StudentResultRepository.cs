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
    public class StudentResultRepository : Repository<StudentResult>, IStudentResultRepository
    {
        public StudentResultRepository(BonzerSchoolDbContext context, ILogger<StudentResultRepository> logger) : base(context,logger)
        {

        }
    }
}
