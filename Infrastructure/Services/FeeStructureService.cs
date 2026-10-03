using Application.Abstractions;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.ViewModels.FeeStructure;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class FeeStructureService : IFeeStructureService
    {
        private readonly ILogger<FeeStructureService> _logger;
        private readonly IFeeStructureRepository _feeStructureRepo;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        public FeeStructureService(
            ILogger<FeeStructureService> logger,
            IFeeStructureRepository feeStructureRepo,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICacheService cacheService)
        {
            _logger = logger;
            _feeStructureRepo = feeStructureRepo;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
        }

        public async Task<IEnumerable<FeeStructureResponse>> GetAllAsync()
        {
            _logger.LogInformation("Fetching all FeeStructures");
            var entities = await _feeStructureRepo.GetAllAsync();
            return _mapper.Map<IEnumerable<FeeStructureResponse>>(entities);
        }

        public async Task<FeeStructureResponse> GetByIdAsync(Guid id)
        {
            _logger.LogInformation($"Fetching FeeStructure by id: {id}");
            var entity = await _feeStructureRepo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<FeeStructureResponse>(entity);
        }

        public async Task<FeeStructureResponse> CreateAsync(FeeStructureRequest request)
        {
            _logger.LogInformation("Creating FeeStructure");
            var entity = _mapper.Map<FeeStructure>(request);

            await _feeStructureRepo.AddAsync(entity);
            await _unitOfWork.Commit();

            _cacheService.Remove("FeeStructures:all");

            return _mapper.Map<FeeStructureResponse>(entity);
        }

        public async Task<FeeStructureResponse> UpdateAsync(Guid id, FeeStructureRequest request)
        {
            _logger.LogInformation($"Updating FeeStructure id: {id}");
            var entity = await _feeStructureRepo.GetByIdAsync(id);
            if (entity == null) return null;

            _mapper.Map(request, entity);
            await _feeStructureRepo.UpdateAsync(entity);
            await _unitOfWork.Commit();

            _cacheService.Remove("FeeStructures:all");
            _cacheService.Remove($"FeeStructure:{id}");

            return _mapper.Map<FeeStructureResponse>(entity);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation($"Deleting FeeStructure id: {id}");
            await _feeStructureRepo.DeleteAsync(id);
            await _unitOfWork.Commit();

            _cacheService.Remove("FeeStructures:all");
            _cacheService.Remove($"FeeStructure:{id}");

            return true;
        }
    }
}