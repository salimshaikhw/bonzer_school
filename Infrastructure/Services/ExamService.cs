using Application.Abstractions;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.ViewModels.Exam;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class ExamService : IExamService
    {
        private readonly ILogger<ExamService> _logger;
        private readonly IExamRepository _examRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        public ExamService(
            ILogger<ExamService> logger,
            IExamRepository examRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICacheService cacheService)
        {
            _logger = logger;
            _examRepository = examRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
        }

        public async Task<IEnumerable<ExamResponse>> GetAllAsync()
        {
            _logger.LogInformation("Fetching all Exams");
            var entities = await _examRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<ExamResponse>>(entities);
        }

        public async Task<ExamResponse> GetByIdAsync(Guid id)
        {
            _logger.LogInformation($"Fetching Exam by id: {id}");
            var entity = await _examRepository.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<ExamResponse>(entity);
        }

        public async Task<ExamResponse> CreateAsync(ExamRequest request)
        {
            _logger.LogInformation("Creating Exam");
            var entity = _mapper.Map<Exam>(request);

            await _examRepository.AddAsync(entity);
            await _unitOfWork.Commit();

            _cacheService.Remove("Exams:all");

            return _mapper.Map<ExamResponse>(entity);
        }

        public async Task<ExamResponse> UpdateAsync(Guid id, ExamRequest request)
        {
            _logger.LogInformation($"Updating Exam id: {id}");
            var entity = await _examRepository.GetByIdAsync(id);
            if (entity == null) return null;

            _mapper.Map(request, entity);
            await _examRepository.UpdateAsync(entity);
            await _unitOfWork.Commit();

            _cacheService.Remove("Exams:all");
            _cacheService.Remove($"Exam:{id}");

            return _mapper.Map<ExamResponse>(entity);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation($"Deleting Exam id: {id}");
            await _examRepository.DeleteAsync(id);
            await _unitOfWork.Commit();

            _cacheService.Remove("Exams:all");
            _cacheService.Remove($"Exam:{id}");

            return true;
        }
    }
}