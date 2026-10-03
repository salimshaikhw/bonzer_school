using Application.Abstractions;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.ViewModels.Homework;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class HomeworkService : IHomeworkService
    {
        private readonly ILogger<HomeworkService> _logger;
        private readonly IHomeworkRepository _homeworkRepo;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        public HomeworkService(
            ILogger<HomeworkService> logger,
            IHomeworkRepository homeworkRepo,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICacheService cacheService)
        {
            _logger = logger;
            _homeworkRepo = homeworkRepo;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
        }

        public async Task<IEnumerable<HomeworkResponse>> GetAllAsync()
        {
            _logger.LogInformation("Fetching all Homeworks");
            var entities = await _homeworkRepo.GetAllAsync();
            return _mapper.Map<IEnumerable<HomeworkResponse>>(entities);
        }

        public async Task<HomeworkResponse> GetByIdAsync(Guid id)
        {
            _logger.LogInformation($"Fetching Homework by id: {id}");
            var entity = await _homeworkRepo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<HomeworkResponse>(entity);
        }

        public async Task<HomeworkResponse> CreateAsync(HomeworkRequest request)
        {
            _logger.LogInformation("Creating Homework");
            var entity = _mapper.Map<Homework>(request);

            await _homeworkRepo.AddAsync(entity);
            await _unitOfWork.Commit();

            _cacheService.Remove("Homeworks:all");

            return _mapper.Map<HomeworkResponse>(entity);
        }

        public async Task<HomeworkResponse> UpdateAsync(Guid id, HomeworkRequest request)
        {
            _logger.LogInformation($"Updating Homework id: {id}");
            var entity = await _homeworkRepo.GetByIdAsync(id);
            if (entity == null) return null;

            _mapper.Map(request, entity);
            await _homeworkRepo.UpdateAsync(entity);
            await _unitOfWork.Commit();

            _cacheService.Remove("Homeworks:all");
            _cacheService.Remove($"Homework:{id}");

            return _mapper.Map<HomeworkResponse>(entity);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation($"Deleting Homework id: {id}");
            await _homeworkRepo.DeleteAsync(id);
            await _unitOfWork.Commit();

            _cacheService.Remove("Homeworks:all");
            _cacheService.Remove($"Homework:{id}");

            return true;
        }
    }
}