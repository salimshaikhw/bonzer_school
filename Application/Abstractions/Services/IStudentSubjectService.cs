using Application.ViewModels.StudentSubject;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface IStudentSubjectService
    {
        Task<IEnumerable<StudentSubjectResponse>> GetAllAsync();
        Task<StudentSubjectResponse> GetByIdAsync(Guid id);
        Task<StudentSubjectResponse> CreateAsync(StudentSubjectRequest request);
        Task<StudentSubjectResponse> UpdateAsync(Guid id, StudentSubjectRequest request);
        Task<bool> DeleteAsync(Guid id);
    }
}