using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.ViewModels.Branch;

namespace Application.Abstractions.Services
{
    public interface IBranchService
    {
        Task<IEnumerable<BranchResponse>> GetAllAsync();
        Task<BranchResponse?> GetByIdAsync(Guid id);
        Task<BranchResponse> CreateAsync(BranchRequest request);
        Task<BranchResponse> UpdateAsync(Guid id, BranchRequest request);
        Task<bool> DeleteAsync(Guid id);
        Task<int> CommitAsync();
    }
}
