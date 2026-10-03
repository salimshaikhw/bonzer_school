using Application.Abstractions;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.ViewModels.ExamSubjectComponent;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class ExamSubjectComponentService : IExamSubjectComponentService
    {
        private readonly ILogger<ExamSubjectComponentService> _logger;
        private readonly IExamSubjectComponentRepository _examSubjectComponentRepo;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        public ExamSubjectComponentService(
            ILogger<ExamSubjectComponentService> logger,
            IExamSubjectComponentRepository examSubjectComponentRepo,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICacheService cacheService)
        {
            _logger = logger;
            _examSubjectComponentRepo = examSubjectComponentRepo;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
        }

        public async Task<IEnumerable<ExamSubjectComponentResponse>> GetAllAsync()
        {
            _logger.LogInformation("Fetching all ExamSubjectComponents");
            var entities = await _examSubjectComponentRepo.GetAllAsync();
            return _mapper.Map<IEnumerable<ExamSubjectComponentResponse>>(entities);
        }

        public async Task<ExamSubjectComponentResponse> GetByIdAsync(Guid id)
        {
            _logger.LogInformation($"Fetching ExamSubjectComponent by id: {id}");
            var entity = await _examSubjectComponentRepo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<ExamSubjectComponentResponse>(entity);
        }

        public async Task<ExamSubjectComponentResponse> CreateAsync(ExamSubjectComponentRequest request)
        {
            _logger.LogInformation("Creating ExamSubjectComponent");
            var entity = _mapper.Map<ExamSubjectComponent>(request);

            await _examSubjectComponentRepo.AddAsync(entity);
            await _unitOfWork.Commit();

            _cacheService.Remove("ExamSubjectComponents:all");

            return _mapper.Map<ExamSubjectComponentResponse>(entity);
        }

        public async Task<ExamSubjectComponentResponse> UpdateAsync(Guid id, ExamSubjectComponentRequest request)
        {
            _logger.LogInformation($"Updating ExamSubjectComponent id: {id}");
            var entity = await _examSubjectComponentRepo.GetByIdAsync(id);
            if (entity == null) return null;

            _mapper.Map(request, entity);
            await _examSubjectComponentRepo.UpdateAsync(entity);
            await _unitOfWork.Commit();

            _cacheService.Remove("ExamSubjectComponents:all");
            _cacheService.Remove($"ExamSubjectComponent:{id}");

            return _mapper.Map<ExamSubjectComponentResponse>(entity);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation($"Deleting ExamSubjectComponent id: {id}");
            await _examSubjectComponentRepo.DeleteAsync(id);
            await _unitOfWork.Commit();

            _cacheService.Remove("ExamSubjectComponents:all");
            _cacheService.Remove($"ExamSubjectComponent:{id}");

            return true;
        }
    }
}