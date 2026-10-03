using Application.ViewModels.GradeMaster;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface IGradeMasterService
    {
        Task<IEnumerable<GradeMasterResponse>> GetAllAsync();
        Task<GradeMasterResponse> GetByIdAsync(Guid id);
        Task<GradeMasterResponse> CreateAsync(GradeMasterRequest request);
        Task<GradeMasterResponse> UpdateAsync(Guid id, GradeMasterRequest request);
        Task<bool> DeleteAsync(Guid id);
    }
}