using Application.ViewModels.Mark;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface IMarkService
    {
        Task<IEnumerable<MarkResponse>> GetAllAsync();
        Task<IEnumerable<MarkResponse>> GetByStudentIdAsync(Guid studentId);
        Task<MarkResponse> GetByIdAsync(Guid id);
        Task<MarkResponse> CreateAsync(MarkRequest request);
        Task<MarkResponse> UpdateAsync(Guid id, MarkRequest request);
        Task<bool> DeleteAsync(Guid id);
        Task<int> CommitAsync();
    }
}