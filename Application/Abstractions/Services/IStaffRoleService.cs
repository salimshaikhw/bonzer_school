using Application.ViewModels.StaffRole;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface IStaffRoleService
    {
        Task<IEnumerable<StaffRoleResponse>> GetAllAsync();
        Task<StaffRoleResponse> GetByIdAsync(Guid id);
        Task<StaffRoleResponse> CreateAsync(StaffRoleRequest request);
        Task<StaffRoleResponse> UpdateAsync(Guid id, StaffRoleRequest request);
        Task<bool> DeleteAsync(Guid id);
    }
}