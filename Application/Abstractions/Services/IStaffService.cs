using Application.ViewModels.Staff;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface IStaffService
    {
        Task<IEnumerable<StaffResponse>> GetAllAsync();
        Task<StaffResponse> GetByIdAsync(Guid id);
        Task<StaffResponse> CreateAsync(StaffRequest request);
        Task<StaffResponse> UpdateAsync(Guid id, StaffRequest request);
        Task<bool> DeleteAsync(Guid id);
    }
}