using Application.Abstractions;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.ViewModels.Subject;
using AutoMapper;
using Domain.Entities;
using Domain.Entities.Postgres.Scaffolded;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class SubjectService : ISubjectService
    {
        private readonly ILogger<SubjectService> _logger;
        private readonly ISubjectRepository _subjectRepo;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public SubjectService(
            ILogger<SubjectService> logger,
            ISubjectRepository subjectRepo,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _logger = logger;
            _subjectRepo = subjectRepo;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<SubjectResponse>> GetAllAsync()
        {
            _logger.LogInformation("Fetching all Subjects");
            var entities = await _subjectRepo.GetAllAsync();
            return _mapper.Map<IEnumerable<SubjectResponse>>(entities);
        }

        public async Task<SubjectResponse> GetByIdAsync(Guid id)
        {
            _logger.LogInformation($"Fetching Subject by id: {id}");
            var entity = await _subjectRepo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<SubjectResponse>(entity);
        }

        public async Task<SubjectResponse> CreateAsync(SubjectRequest request)
        {
            _logger.LogInformation("Creating Subject");
            var entity = _mapper.Map<Subject>(request);

            await _subjectRepo.AddAsync(entity);
            await _unitOfWork.Commit();

            return _mapper.Map<SubjectResponse>(entity);
        }

        public async Task<SubjectResponse> UpdateAsync(Guid id, SubjectRequest request)
        {
            _logger.LogInformation($"Updating Subject id: {id}");
            var entity = await _subjectRepo.GetByIdAsync(id);
            if (entity == null) return null;

            _mapper.Map(request, entity);
            await _subjectRepo.UpdateAsync(entity);
            await _unitOfWork.Commit();

            return _mapper.Map<SubjectResponse>(entity);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation($"Deleting Subject id: {id}");
            await _subjectRepo.DeleteAsync(id);
            await _unitOfWork.Commit();

            return true;
        }
    }
}