using Application.Abstractions;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.ViewModels.ExamType;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class ExamTypeService : IExamTypeService
    {
        private readonly ILogger<ExamTypeService> _logger;
        private readonly IExamTypeRepository _examTypeRepo;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        public ExamTypeService(
            ILogger<ExamTypeService> logger,
            IExamTypeRepository examTypeRepo,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICacheService cacheService)
        {
            _logger = logger;
            _examTypeRepo = examTypeRepo;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
        }

        public async Task<IEnumerable<ExamTypeResponse>> GetAllAsync()
        {
            _logger.LogInformation("Fetching all ExamTypes");
            var entities = await _examTypeRepo.GetAllAsync();
            return _mapper.Map<IEnumerable<ExamTypeResponse>>(entities);
        }

        public async Task<ExamTypeResponse> GetByIdAsync(Guid id)
        {
            _logger.LogInformation($"Fetching ExamType by id: {id}");
            var entity = await _examTypeRepo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<ExamTypeResponse>(entity);
        }

        public async Task<ExamTypeResponse> CreateAsync(ExamTypeRequest request)
        {
            _logger.LogInformation("Creating ExamType");
            var entity = _mapper.Map<ExamType>(request);

            await _examTypeRepo.AddAsync(entity);
            await _unitOfWork.Commit();

            _cacheService.Remove("ExamTypes:all");

            return _mapper.Map<ExamTypeResponse>(entity);
        }

        public async Task<ExamTypeResponse> UpdateAsync(Guid id, ExamTypeRequest request)
        {
            _logger.LogInformation($"Updating ExamType id: {id}");
            var entity = await _examTypeRepo.GetByIdAsync(id);
            if (entity == null) return null;

            _mapper.Map(request, entity);
            await _examTypeRepo.UpdateAsync(entity);
            await _unitOfWork.Commit();

            _cacheService.Remove("ExamTypes:all");
            _cacheService.Remove($"ExamType:{id}");

            return _mapper.Map<ExamTypeResponse>(entity);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation($"Deleting ExamType id: {id}");
            await _examTypeRepo.DeleteAsync(id);
            await _unitOfWork.Commit();

            _cacheService.Remove("ExamTypes:all");
            _cacheService.Remove($"ExamType:{id}");

            return true;
        }
    }
}