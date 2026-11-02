using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Abstractions.Repositories.Students
{
    public interface IStudentRepository : IRepository<Student>
    {
        Task<(List<Student> Items, int TotalCount)> GetPagedAsync(int page, int pageSize);
    }
}
