using Application.Abstractions;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.ViewModels.TeacherSubjectMap;
using AutoMapper;
using Domain.Entities;
using Domain.Entities.Postgres.Scaffolded;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class TeacherSubjectMapService : ITeacherSubjectMapService
    {
        private readonly ILogger<TeacherSubjectMapService> _logger;
        private readonly ITeacherSubjectMapRepository _teacherSubjectMapRepo;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public TeacherSubjectMapService(
            ILogger<TeacherSubjectMapService> logger,
            ITeacherSubjectMapRepository teacherSubjectMapRepo,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _logger = logger;
            _teacherSubjectMapRepo = teacherSubjectMapRepo;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<TeacherSubjectMapResponse>> GetAllAsync()
        {
            _logger.LogInformation("Fetching all TeacherSubject mappings");
            var entities = await _teacherSubjectMapRepo.GetAllAsync();
            return _mapper.Map<IEnumerable<TeacherSubjectMapResponse>>(entities);
        }

        public async Task<TeacherSubjectMapResponse> GetByIdAsync(Guid id)
        {
            _logger.LogInformation($"Fetching TeacherSubjectMap by id: {id}");
            var entity = await _teacherSubjectMapRepo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<TeacherSubjectMapResponse>(entity);
        }

        public async Task<TeacherSubjectMapResponse> CreateAsync(TeacherSubjectMapRequest request)
        {
            _logger.LogInformation("Creating TeacherSubjectMap");
            var entity = _mapper.Map<TeacherSubjectMap>(request);

            await _teacherSubjectMapRepo.AddAsync(entity);
            await _unitOfWork.Commit();

            return _mapper.Map<TeacherSubjectMapResponse>(entity);
        }

        public async Task<TeacherSubjectMapResponse> UpdateAsync(Guid id, TeacherSubjectMapRequest request)
        {
            _logger.LogInformation($"Updating TeacherSubjectMap id: {id}");
            var entity = await _teacherSubjectMapRepo.GetByIdAsync(id);
            if (entity == null) return null;

            _mapper.Map(request, entity);
            await _teacherSubjectMapRepo.UpdateAsync(entity);
            await _unitOfWork.Commit();

            return _mapper.Map<TeacherSubjectMapResponse>(entity);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation($"Deleting TeacherSubjectMap id: {id}");
            await _teacherSubjectMapRepo.DeleteAsync(id);
            await _unitOfWork.Commit();

            return true;
        }
    }
}