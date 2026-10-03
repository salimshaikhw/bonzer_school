using Application.ViewModels.ExamSubjectComponent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface IExamSubjectComponentService
    {
        Task<IEnumerable<ExamSubjectComponentResponse>> GetAllAsync();
        Task<ExamSubjectComponentResponse> GetByIdAsync(Guid id);
        Task<ExamSubjectComponentResponse> CreateAsync(ExamSubjectComponentRequest request);
        Task<ExamSubjectComponentResponse> UpdateAsync(Guid id, ExamSubjectComponentRequest request);
        Task<bool> DeleteAsync(Guid id);
    }
}