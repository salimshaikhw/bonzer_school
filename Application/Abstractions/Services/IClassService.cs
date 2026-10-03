using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.ViewModels.Class;

namespace Application.Abstractions.Services
{
    public interface IClassService
    {
        Task<IEnumerable<ClassResponse>> GetAllAsync();
        Task<IEnumerable<ClassResponse>> GetByBranchIdAsync(Guid branchId);
        Task<ClassResponse?> GetByIdAsync(Guid id);
        Task<ClassResponse> CreateAsync(ClassRequest request);
        Task<ClassResponse> UpdateAsync(Guid id, ClassRequest request);
        Task<bool> DeleteAsync(Guid id);
        Task<int> CommitAsync();
    }
}
