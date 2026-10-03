using Application.ViewModels.FeeInstallment;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface IFeeInstallmentService
    {
        Task<IEnumerable<FeeInstallmentResponse>> GetAllAsync();
        Task<FeeInstallmentResponse> GetByIdAsync(Guid id);
        Task<FeeInstallmentResponse> CreateAsync(FeeInstallmentRequest request);
        Task<FeeInstallmentResponse> UpdateAsync(Guid id, FeeInstallmentRequest request);
        Task<bool> DeleteAsync(Guid id);
    }
}