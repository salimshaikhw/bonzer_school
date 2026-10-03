using Application.ViewModels.ExamType;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface IExamTypeService
    {
        Task<IEnumerable<ExamTypeResponse>> GetAllAsync();
        Task<ExamTypeResponse> GetByIdAsync(Guid id);
        Task<ExamTypeResponse> CreateAsync(ExamTypeRequest request);
        Task<ExamTypeResponse> UpdateAsync(Guid id, ExamTypeRequest request);
        Task<bool> DeleteAsync(Guid id);
    }
}