using Application.ViewModels.ExamComponentType;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface IExamComponentTypeService
    {
        Task<IEnumerable<ExamComponentTypeResponse>> GetAllAsync();
        Task<ExamComponentTypeResponse> GetByIdAsync(Guid id);
        Task<ExamComponentTypeResponse> CreateAsync(ExamComponentTypeRequest request);
        Task<ExamComponentTypeResponse> UpdateAsync(Guid id, ExamComponentTypeRequest request);
        Task<bool> DeleteAsync(Guid id);
    }
}