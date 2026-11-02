using Application.Abstractions.Repositories;
using Infrastructure.Persistence.Postgres;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories.Exams
{
    public class MarkRepository : Repository<Mark> , IMarkRepository
    {
        public MarkRepository(BonzerSchoolDbContext contex, ILogger<MarkRepository> logger) : base(contex, logger)
        { }
    }
}
