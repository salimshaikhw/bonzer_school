using Application.ViewModels.Role;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface IRoleService
    {
        Task<IEnumerable<RoleResponse>> GetAllAsync();
        Task<RoleResponse> GetByIdAsync(Guid id);
        Task<RoleResponse> CreateAsync(RoleRequest request);
        Task<RoleResponse> UpdateAsync(Guid id, RoleRequest request);
        Task<bool> DeleteAsync(Guid id);
    }
}