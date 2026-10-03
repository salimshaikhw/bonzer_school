using Application.Abstractions;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.ViewModels.FeeInstallment;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class FeeInstallmentService : IFeeInstallmentService
    {
        private readonly ILogger<FeeInstallmentService> _logger;
        private readonly IFeeInstallmentRepository _feeInstallmentRepo;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        public FeeInstallmentService(
            ILogger<FeeInstallmentService> logger,
            IFeeInstallmentRepository feeInstallmentRepo,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICacheService cacheService)
        {
            _logger = logger;
            _feeInstallmentRepo = feeInstallmentRepo;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
        }

        public async Task<IEnumerable<FeeInstallmentResponse>> GetAllAsync()
        {
            _logger.LogInformation("Fetching all FeeInstallments");
            var entities = await _feeInstallmentRepo.GetAllAsync();
            return _mapper.Map<IEnumerable<FeeInstallmentResponse>>(entities);
        }

        public async Task<FeeInstallmentResponse> GetByIdAsync(Guid id)
        {
            _logger.LogInformation($"Fetching FeeInstallment by id: {id}");
            var entity = await _feeInstallmentRepo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<FeeInstallmentResponse>(entity);
        }

        public async Task<FeeInstallmentResponse> CreateAsync(FeeInstallmentRequest request)
        {
            _logger.LogInformation("Creating FeeInstallment");
            var entity = _mapper.Map<FeeInstallment>(request);

            await _feeInstallmentRepo.AddAsync(entity);
            await _unitOfWork.Commit();

            _cacheService.Remove("FeeInstallments:all");

            return _mapper.Map<FeeInstallmentResponse>(entity);
        }

        public async Task<FeeInstallmentResponse> UpdateAsync(Guid id, FeeInstallmentRequest request)
        {
            _logger.LogInformation($"Updating FeeInstallment id: {id}");
            var entity = await _feeInstallmentRepo.GetByIdAsync(id);
            if (entity == null) return null;

            _mapper.Map(request, entity);
            await _feeInstallmentRepo.UpdateAsync(entity);
            await _unitOfWork.Commit();

            _cacheService.Remove("FeeInstallments:all");
            _cacheService.Remove($"FeeInstallment:{id}");

            return _mapper.Map<FeeInstallmentResponse>(entity);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation($"Deleting FeeInstallment id: {id}");
            await _feeInstallmentRepo.DeleteAsync(id);
            await _unitOfWork.Commit();

            _cacheService.Remove("FeeInstallments:all");
            _cacheService.Remove($"FeeInstallment:{id}");

            return true;
        }
    }
}