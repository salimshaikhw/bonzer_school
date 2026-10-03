using Application.ViewModels.Exam;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface IExamService
    {
        Task<IEnumerable<ExamResponse>> GetAllAsync();
        Task<ExamResponse> GetByIdAsync(Guid id);
        Task<ExamResponse> CreateAsync(ExamRequest request);
        Task<ExamResponse> UpdateAsync(Guid id, ExamRequest request);
        Task<bool> DeleteAsync(Guid id);
    }
}