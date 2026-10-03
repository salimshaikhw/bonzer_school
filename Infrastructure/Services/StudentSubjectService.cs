using Application.Abstractions;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.ViewModels.StudentSubject;
using AutoMapper;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class StudentSubjectService : IStudentSubjectService
    {
        private readonly ILogger<StudentSubjectService> _logger;
        private readonly IStudentSubjectRepository _studentSubjectRepo;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public StudentSubjectService(
            ILogger<StudentSubjectService> logger,
            IStudentSubjectRepository studentSubjectRepo,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _logger = logger;
            _studentSubjectRepo = studentSubjectRepo;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<StudentSubjectResponse>> GetAllAsync()
        {
            _logger.LogInformation("Fetching all StudentSubjects");
            var entities = await _studentSubjectRepo.GetAllAsync();
            return _mapper.Map<IEnumerable<StudentSubjectResponse>>(entities);
        }

        public async Task<StudentSubjectResponse> GetByIdAsync(Guid id)
        {
            _logger.LogInformation($"Fetching StudentSubject by id: {id}");
            var entity = await _studentSubjectRepo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<StudentSubjectResponse>(entity);
        }

        public async Task<StudentSubjectResponse> CreateAsync(StudentSubjectRequest request)
        {
            _logger.LogInformation("Creating StudentSubject");
            var entity = _mapper.Map<StudentSubject>(request);

            await _studentSubjectRepo.AddAsync(entity);
            await _unitOfWork.Commit();

            return _mapper.Map<StudentSubjectResponse>(entity);
        }

        public async Task<StudentSubjectResponse> UpdateAsync(Guid id, StudentSubjectRequest request)
        {
            _logger.LogInformation($"Updating StudentSubject id: {id}");
            var entity = await _studentSubjectRepo.GetByIdAsync(id);
            if (entity == null) return null;

            _mapper.Map(request, entity);
            await _studentSubjectRepo.UpdateAsync(entity);
            await _unitOfWork.Commit();

            return _mapper.Map<StudentSubjectResponse>(entity);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation($"Deleting StudentSubject id: {id}");
            await _studentSubjectRepo.DeleteAsync(id);
            await _unitOfWork.Commit();

            return true;
        }
    }
}