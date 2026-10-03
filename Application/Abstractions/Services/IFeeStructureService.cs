using Application.ViewModels.FeeStructure;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface IFeeStructureService
    {
        Task<IEnumerable<FeeStructureResponse>> GetAllAsync();
        Task<FeeStructureResponse> GetByIdAsync(Guid id);
        Task<FeeStructureResponse> CreateAsync(FeeStructureRequest request);
        Task<FeeStructureResponse> UpdateAsync(Guid id, FeeStructureRequest request);
        Task<bool> DeleteAsync(Guid id);
    }
}