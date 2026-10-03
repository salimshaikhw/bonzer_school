using Application.ViewModels.Timetable;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface ITimetableService
    {
        Task<IEnumerable<TimetableResponse>> GetAllAsync();
        Task<TimetableResponse> GetByIdAsync(Guid id);
        Task<TimetableResponse> CreateAsync(TimetableRequest request);
        Task<TimetableResponse> UpdateAsync(Guid id, TimetableRequest request);
        Task<bool> DeleteAsync(Guid id);
    }
}