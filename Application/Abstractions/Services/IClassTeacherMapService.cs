using Application.ViewModels.ClassTeacherMap;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface IClassTeacherMapService
    {
        Task<IEnumerable<ClassTeacherMapResponse>> GetAllAsync();
        Task<ClassTeacherMapResponse> GetByIdAsync(Guid id);
        Task<ClassTeacherMapResponse> CreateAsync(ClassTeacherMapRequest request);
        Task<ClassTeacherMapResponse> UpdateAsync(Guid id, ClassTeacherMapRequest request);
        Task<bool> DeleteAsync(Guid id);
    }
}