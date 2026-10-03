using Application.ViewModels.StudentResult;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface IStudentResultService
    {
        Task<IEnumerable<StudentResultResponse>> GetAllAsync();
        Task<StudentResultResponse> GetByIdAsync(Guid id);
        Task<StudentResultResponse> CreateAsync(StudentResultRequest request);
        Task<StudentResultResponse> UpdateAsync(Guid id, StudentResultRequest request);
        Task<bool> DeleteAsync(Guid id);
    }
}