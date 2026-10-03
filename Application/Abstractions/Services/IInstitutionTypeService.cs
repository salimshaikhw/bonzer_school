using Application.ViewModels.InstitutionType;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface IInstitutionTypeService
    {
        Task<IEnumerable<InstitutionTypeResponse>> GetAllAsync();
        Task<InstitutionTypeResponse> GetByIdAsync(Guid id);
        Task<InstitutionTypeResponse> CreateAsync(InstitutionTypeRequest request);
        Task<InstitutionTypeResponse> UpdateAsync(Guid id, InstitutionTypeRequest request);
        Task<bool> DeleteAsync(Guid id);
    }
}