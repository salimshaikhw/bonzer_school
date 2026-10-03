using Application.ViewModels.Division;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface IDivisionService
    {
        Task<IEnumerable<DivisionResponse>> GetAllAsync();
        Task<IEnumerable<DivisionResponse>> GetByClassIdAsync(Guid classId);
        Task<DivisionResponse> GetByIdAsync(Guid id);
        Task<DivisionResponse> CreateAsync(DivisionRequest request);
        Task<DivisionResponse> UpdateAsync(Guid id, DivisionRequest request);
        Task<bool> DeleteAsync(Guid id);
    }
}