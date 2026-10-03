using Application.ViewModels.Medium;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface IMediumService
    {
        Task<IEnumerable<MediumResponse>> GetAllAsync();
        Task<MediumResponse> GetByIdAsync(Guid id);
        Task<MediumResponse> CreateAsync(MediumRequest request);
        Task<MediumResponse> UpdateAsync(Guid id, MediumRequest request);
        Task<bool> DeleteAsync(Guid id);
    }
}