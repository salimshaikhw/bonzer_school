using Application.ViewModels.Subject;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface ISubjectService
    {
        Task<IEnumerable<SubjectResponse>> GetAllAsync();
        Task<SubjectResponse> GetByIdAsync(Guid id);
        Task<SubjectResponse> CreateAsync(SubjectRequest request);
        Task<SubjectResponse> UpdateAsync(Guid id, SubjectRequest request);
        Task<bool> DeleteAsync(Guid id);
    }
}