using Application.Abstractions;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.ViewModels.GradeMaster;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class GradeMasterService : IGradeMasterService
    {
        private readonly ILogger<GradeMasterService> _logger;
        private readonly IGradeMasterRepository _gradeMasterRepo;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        public GradeMasterService(
            ILogger<GradeMasterService> logger,
            IGradeMasterRepository gradeMasterRepo,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICacheService cacheService)
        {
            _logger = logger;
            _gradeMasterRepo = gradeMasterRepo;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
        }

        public async Task<IEnumerable<GradeMasterResponse>> GetAllAsync()
        {
            _logger.LogInformation("Fetching all GradeMasters");
            var entities = await _gradeMasterRepo.GetAllAsync();
            return _mapper.Map<IEnumerable<GradeMasterResponse>>(entities);
        }

        public async Task<GradeMasterResponse> GetByIdAsync(Guid id)
        {
            _logger.LogInformation($"Fetching GradeMaster by id: {id}");
            var entity = await _gradeMasterRepo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<GradeMasterResponse>(entity);
        }

        public async Task<GradeMasterResponse> CreateAsync(GradeMasterRequest request)
        {
            _logger.LogInformation("Creating GradeMaster");
            var entity = _mapper.Map<GradeMaster>(request);

            await _gradeMasterRepo.AddAsync(entity);
            await _unitOfWork.Commit();

            _cacheService.Remove("GradeMasters:all");

            return _mapper.Map<GradeMasterResponse>(entity);
        }

        public async Task<GradeMasterResponse> UpdateAsync(Guid id, GradeMasterRequest request)
        {
            _logger.LogInformation($"Updating GradeMaster id: {id}");
            var entity = await _gradeMasterRepo.GetByIdAsync(id);
            if (entity == null) return null;

            _mapper.Map(request, entity);
            await _gradeMasterRepo.UpdateAsync(entity);
            await _unitOfWork.Commit();

            _cacheService.Remove("GradeMasters:all");
            _cacheService.Remove($"GradeMaster:{id}");

            return _mapper.Map<GradeMasterResponse>(entity);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation($"Deleting GradeMaster id: {id}");
            await _gradeMasterRepo.DeleteAsync(id);
            await _unitOfWork.Commit();

            _cacheService.Remove("GradeMasters:all");
            _cacheService.Remove($"GradeMaster:{id}");

            return true;
        }
    }
}