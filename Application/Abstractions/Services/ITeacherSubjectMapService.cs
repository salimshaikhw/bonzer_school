using Application.ViewModels.TeacherSubjectMap;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface ITeacherSubjectMapService
    {
        Task<IEnumerable<TeacherSubjectMapResponse>> GetAllAsync();
        Task<TeacherSubjectMapResponse> GetByIdAsync(Guid id);
        Task<TeacherSubjectMapResponse> CreateAsync(TeacherSubjectMapRequest request);
        Task<TeacherSubjectMapResponse> UpdateAsync(Guid id, TeacherSubjectMapRequest request);
        Task<bool> DeleteAsync(Guid id);
    }
}