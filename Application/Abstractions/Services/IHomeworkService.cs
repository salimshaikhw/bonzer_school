using Application.ViewModels.Homework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface IHomeworkService
    {
        Task<IEnumerable<HomeworkResponse>> GetAllAsync();
        Task<HomeworkResponse> GetByIdAsync(Guid id);
        Task<HomeworkResponse> CreateAsync(HomeworkRequest request);
        Task<HomeworkResponse> UpdateAsync(Guid id, HomeworkRequest request);
        Task<bool> DeleteAsync(Guid id);
    }
}