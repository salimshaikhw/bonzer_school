using Application.ViewModels.Notification;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface INotificationService
    {
        Task<IEnumerable<NotificationResponse>> GetAllAsync();
        Task<NotificationResponse> GetByIdAsync(Guid id);
        Task<NotificationResponse> CreateAsync(NotificationRequest request);
        Task<NotificationResponse> UpdateAsync(Guid id, NotificationRequest request);
        Task<bool> DeleteAsync(Guid id);
    }
}