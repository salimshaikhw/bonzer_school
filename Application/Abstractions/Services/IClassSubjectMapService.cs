using Application.ViewModels.ClassSubjectMap;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface IClassSubjectMapService
    {
        Task<IEnumerable<ClassSubjectMapResponse>> GetAllAsync();
        Task<ClassSubjectMapResponse> GetByIdAsync(Guid id);
        Task<ClassSubjectMapResponse> CreateAsync(ClassSubjectMapRequest request);
        Task<ClassSubjectMapResponse> UpdateAsync(Guid id, ClassSubjectMapRequest request);
        Task<bool> DeleteAsync(Guid id);
    }
}