using Application.ViewModels.ParentUser;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface IParentUserService
    {
        Task<IEnumerable<ParentUserResponse>> GetAllAsync();
        Task<ParentUserResponse> GetByIdAsync(Guid id);
        Task<ParentUserResponse> CreateAsync(ParentUserRequest request);
        Task<ParentUserResponse> UpdateAsync(Guid id, ParentUserRequest request);
        Task<bool> DeleteAsync(Guid id);
    }
}