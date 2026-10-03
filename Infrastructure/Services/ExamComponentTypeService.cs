using Application.Abstractions;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.ViewModels.ExamComponentType;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class ExamComponentTypeService : IExamComponentTypeService
    {
        private readonly ILogger<ExamComponentTypeService> _logger;
        private readonly IExamComponentTypeRepository _examComponentTypeRepo;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        public ExamComponentTypeService(
            ILogger<ExamComponentTypeService> logger,
            IExamComponentTypeRepository examComponentTypeRepo,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICacheService cacheService)
        {
            _logger = logger;
            _examComponentTypeRepo = examComponentTypeRepo;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
        }

        public async Task<IEnumerable<ExamComponentTypeResponse>> GetAllAsync()
        {
            _logger.LogInformation("Fetching all ExamComponentTypes");
            var entities = await _examComponentTypeRepo.GetAllAsync();
            return _mapper.Map<IEnumerable<ExamComponentTypeResponse>>(entities);
        }

        public async Task<ExamComponentTypeResponse> GetByIdAsync(Guid id)
        {
            _logger.LogInformation($"Fetching ExamComponentType by id: {id}");
            var entity = await _examComponentTypeRepo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<ExamComponentTypeResponse>(entity);
        }

        public async Task<ExamComponentTypeResponse> CreateAsync(ExamComponentTypeRequest request)
        {
            _logger.LogInformation("Creating ExamComponentType");
            var entity = _mapper.Map<ExamComponentType>(request);

            await _examComponentTypeRepo.AddAsync(entity);
            await _unitOfWork.Commit();

            _cacheService.Remove("ExamComponentTypes:all");

            return _mapper.Map<ExamComponentTypeResponse>(entity);
        }

        public async Task<ExamComponentTypeResponse> UpdateAsync(Guid id, ExamComponentTypeRequest request)
        {
            _logger.LogInformation($"Updating ExamComponentType id: {id}");
            var entity = await _examComponentTypeRepo.GetByIdAsync(id);
            if (entity == null) return null;

            _mapper.Map(request, entity);
            await _examComponentTypeRepo.UpdateAsync(entity);
            await _unitOfWork.Commit();

            _cacheService.Remove("ExamComponentTypes:all");
            _cacheService.Remove($"ExamComponentType:{id}");

            return _mapper.Map<ExamComponentTypeResponse>(entity);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation($"Deleting ExamComponentType id: {id}");
            await _examComponentTypeRepo.DeleteAsync(id);
            await _unitOfWork.Commit();

            _cacheService.Remove("ExamComponentTypes:all");
            _cacheService.Remove($"ExamComponentType:{id}");

            return true;
        }
    }
}