using Application.ViewModels.ExamSubject;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface IExamSubjectService
    {
        Task<IEnumerable<ExamSubjectResponse>> GetAllAsync();
        Task<ExamSubjectResponse> GetByIdAsync(Guid id);
        Task<ExamSubjectResponse> CreateAsync(ExamSubjectRequest request);
        Task<ExamSubjectResponse> UpdateAsync(Guid id, ExamSubjectRequest request);
        Task<bool> DeleteAsync(Guid id);
    }
}