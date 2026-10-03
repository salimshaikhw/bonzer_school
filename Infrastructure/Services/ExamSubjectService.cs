using Application.Abstractions;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.ViewModels.ExamSubject;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class ExamSubjectService : IExamSubjectService
    {
        private readonly ILogger<ExamSubjectService> _logger;
        private readonly IExamSubjectRepository _examSubjectRepo;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        public ExamSubjectService(
            ILogger<ExamSubjectService> logger,
            IExamSubjectRepository examSubjectRepo,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICacheService cacheService)
        {
            _logger = logger;
            _examSubjectRepo = examSubjectRepo;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
        }

        public async Task<IEnumerable<ExamSubjectResponse>> GetAllAsync()
        {
            _logger.LogInformation("Fetching all ExamSubjects");
            var entities = await _examSubjectRepo.GetAllAsync();
            return _mapper.Map<IEnumerable<ExamSubjectResponse>>(entities);
        }

        public async Task<ExamSubjectResponse> GetByIdAsync(Guid id)
        {
            _logger.LogInformation($"Fetching ExamSubject by id: {id}");
            var entity = await _examSubjectRepo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<ExamSubjectResponse>(entity);
        }

        public async Task<ExamSubjectResponse> CreateAsync(ExamSubjectRequest request)
        {
            _logger.LogInformation("Creating ExamSubject");
            var entity = _mapper.Map<ExamSubject>(request);

            await _examSubjectRepo.AddAsync(entity);
            await _unitOfWork.Commit();

            _cacheService.Remove("ExamSubjects:all");

            return _mapper.Map<ExamSubjectResponse>(entity);
        }

        public async Task<ExamSubjectResponse> UpdateAsync(Guid id, ExamSubjectRequest request)
        {
            _logger.LogInformation($"Updating ExamSubject id: {id}");
            var entity = await _examSubjectRepo.GetByIdAsync(id);
            if (entity == null) return null;

            _mapper.Map(request, entity);
            await _examSubjectRepo.UpdateAsync(entity);
            await _unitOfWork.Commit();

            _cacheService.Remove("ExamSubjects:all");
            _cacheService.Remove($"ExamSubject:{id}");

            return _mapper.Map<ExamSubjectResponse>(entity);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation($"Deleting ExamSubject id: {id}");
            await _examSubjectRepo.DeleteAsync(id);
            await _unitOfWork.Commit();

            _cacheService.Remove("ExamSubjects:all");
            _cacheService.Remove($"ExamSubject:{id}");

            return true;
        }
    }
}