using Application.ViewModels.ClassFeeMap;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface IClassFeeMapService
    {
        Task<IEnumerable<ClassFeeMapResponse>> GetAllAsync();
        Task<ClassFeeMapResponse> GetByIdAsync(Guid id);
        Task<ClassFeeMapResponse> CreateAsync(ClassFeeMapRequest request);
        Task<ClassFeeMapResponse> UpdateAsync(Guid id, ClassFeeMapRequest request);
        Task<bool> DeleteAsync(Guid id);
    }
}