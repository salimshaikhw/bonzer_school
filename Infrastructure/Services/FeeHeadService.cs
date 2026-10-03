using Application.Abstractions;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.ViewModels.FeeHead;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class FeeHeadService : IFeeHeadService
    {
        private readonly ILogger<FeeHeadService> _logger;
        private readonly IFeeHeadRepository _feeHeadRepo;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        public FeeHeadService(
            ILogger<FeeHeadService> logger,
            IFeeHeadRepository feeHeadRepo,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICacheService cacheService)
        {
            _logger = logger;
            _feeHeadRepo = feeHeadRepo;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
        }

        public async Task<IEnumerable<FeeHeadResponse>> GetAllAsync()
        {
            _logger.LogInformation("Fetching all FeeHeads");
            var entities = await _feeHeadRepo.GetAllAsync();
            return _mapper.Map<IEnumerable<FeeHeadResponse>>(entities);
        }

        public async Task<FeeHeadResponse> GetByIdAsync(Guid id)
        {
            _logger.LogInformation($"Fetching FeeHead by id: {id}");
            var entity = await _feeHeadRepo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<FeeHeadResponse>(entity);
        }

        public async Task<FeeHeadResponse> CreateAsync(FeeHeadRequest request)
        {
            _logger.LogInformation("Creating FeeHead");
            var entity = _mapper.Map<FeeHead>(request);

            await _feeHeadRepo.AddAsync(entity);
            await _unitOfWork.Commit();

            _cacheService.Remove("FeeHeads:all");

            return _mapper.Map<FeeHeadResponse>(entity);
        }

        public async Task<FeeHeadResponse> UpdateAsync(Guid id, FeeHeadRequest request)
        {
            _logger.LogInformation($"Updating FeeHead id: {id}");
            var entity = await _feeHeadRepo.GetByIdAsync(id);
            if (entity == null) return null;

            _mapper.Map(request, entity);
            await _feeHeadRepo.UpdateAsync(entity);
            await _unitOfWork.Commit();

            _cacheService.Remove("FeeHeads:all");
            _cacheService.Remove($"FeeHead:{id}");

            return _mapper.Map<FeeHeadResponse>(entity);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation($"Deleting FeeHead id: {id}");
            await _feeHeadRepo.DeleteAsync(id);
            await _unitOfWork.Commit();

            _cacheService.Remove("FeeHeads:all");
            _cacheService.Remove($"FeeHead:{id}");

            return true;
        }
    }
}