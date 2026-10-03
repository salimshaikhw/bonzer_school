using Application.Abstractions.Repositories;
using Domain.Entities.Postgres.Scaffolded;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Infrastructure.Persistence.Postgres;

namespace Infrastructure.Persistence.Repositories
{
    public class StudentRepository : Repository<Student>, IStudentRepository
    {
        public StudentRepository(BonzerSchoolDbContext context, ILogger<StudentRepository> logger)
            : base(context, logger)
        {
        }

        public async Task<(List<Student> Items, int TotalCount)> GetPagedAsync(int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var query = _dbSet.AsNoTracking().OrderBy(s => s.LastName ?? "").ThenBy(s => s.FirstName ?? "").ThenBy(s => s.Id);
            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, total);
        }
    }
}
