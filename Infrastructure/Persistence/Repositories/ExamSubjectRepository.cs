using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Infrastructure.Persistence.Postgres;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories
{
    public class ExamSubjectRepository : Repository<ExamSubject>, IExamSubjectRepository
    {
        public ExamSubjectRepository(BonzerSchoolDbContext context, ILogger<ExamSubjectRepository> logger) : base(context,logger)
        {

        }
    }
}
