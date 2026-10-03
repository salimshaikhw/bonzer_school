using Application.ViewModels.Department;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface IDepartmentService
    {
        Task<IEnumerable<DepartmentResponse>> GetAllAsync();
        Task<DepartmentResponse> GetByIdAsync(Guid id);
        Task<DepartmentResponse> CreateAsync(DepartmentRequest request);
        Task<DepartmentResponse> UpdateAsync(Guid id, DepartmentRequest request);
        Task<bool> DeleteAsync(Guid id);
    }
}