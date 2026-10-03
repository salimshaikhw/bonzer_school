using Application.ViewModels.FeeHead;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface IFeeHeadService
    {
        Task<IEnumerable<FeeHeadResponse>> GetAllAsync();
        Task<FeeHeadResponse> GetByIdAsync(Guid id);
        Task<FeeHeadResponse> CreateAsync(FeeHeadRequest request);
        Task<FeeHeadResponse> UpdateAsync(Guid id, FeeHeadRequest request);
        Task<bool> DeleteAsync(Guid id);
    }
}