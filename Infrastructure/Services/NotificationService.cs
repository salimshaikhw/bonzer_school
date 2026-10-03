using Application.Abstractions;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.ViewModels.Notification;
using AutoMapper;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly ILogger<NotificationService> _logger;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public NotificationService(
            INotificationRepository notificationRepository,
            ILogger<NotificationService> logger,
            IMapper mapper,
            IUnitOfWork unitOfWork)
        {
            _notificationRepository = notificationRepository;
            _logger = logger;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<NotificationResponse>> GetAllAsync()
        {
            _logger.LogInformation("Fetching all Notifications");
            var entities = await _notificationRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<NotificationResponse>>(entities);
        }

        public async Task<NotificationResponse> GetByIdAsync(Guid id)
        {
            _logger.LogInformation($"Fetching Notification by id: {id}");
            var entity = await _notificationRepository.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<NotificationResponse>(entity);
        }

        public async Task<NotificationResponse> CreateAsync(NotificationRequest request)
        {
            _logger.LogInformation("Creating Notification");
            var entity = _mapper.Map<Notification>(request);

            await _notificationRepository.AddAsync(entity);
            await _unitOfWork.Commit();

            return _mapper.Map<NotificationResponse>(entity);
        }

        public async Task<NotificationResponse> UpdateAsync(Guid id, NotificationRequest request)
        {
            _logger.LogInformation($"Updating Notification id: {id}");
            var entity = await _notificationRepository.GetByIdAsync(id);
            if (entity == null) return null;

            _mapper.Map(request, entity);
            await _notificationRepository.UpdateAsync(entity);
            await _unitOfWork.Commit();

            return _mapper.Map<NotificationResponse>(entity);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation($"Deleting Notification id: {id}");
            await _notificationRepository.DeleteAsync(id);
            await _unitOfWork.Commit();

            return true;
        }
    }
}