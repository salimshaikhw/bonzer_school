using Application.Abstractions;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.ViewModels.StudentResult;
using AutoMapper;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class StudentResultService : IStudentResultService
    {
        private readonly ILogger<StudentResultService> _logger;
        private readonly IStudentResultRepository _studentResultRepo;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public StudentResultService(
            ILogger<StudentResultService> logger,
            IStudentResultRepository studentResultRepo,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _logger = logger;
            _studentResultRepo = studentResultRepo;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<StudentResultResponse>> GetAllAsync()
        {
            _logger.LogInformation("Fetching all StudentResults");
            var entities = await _studentResultRepo.GetAllAsync();
            return _mapper.Map<IEnumerable<StudentResultResponse>>(entities);
        }

        public async Task<StudentResultResponse> GetByIdAsync(Guid id)
        {
            _logger.LogInformation($"Fetching StudentResult by id: {id}");
            var entity = await _studentResultRepo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<StudentResultResponse>(entity);
        }

        public async Task<StudentResultResponse> CreateAsync(StudentResultRequest request)
        {
            _logger.LogInformation("Creating StudentResult");
            var entity = _mapper.Map<StudentResult>(request);

            await _studentResultRepo.AddAsync(entity);
            await _unitOfWork.Commit();

            return _mapper.Map<StudentResultResponse>(entity);
        }

        public async Task<StudentResultResponse> UpdateAsync(Guid id, StudentResultRequest request)
        {
            _logger.LogInformation($"Updating StudentResult id: {id}");
            var entity = await _studentResultRepo.GetByIdAsync(id);
            if (entity == null) return null;

            _mapper.Map(request, entity);
            await _studentResultRepo.UpdateAsync(entity);
            await _unitOfWork.Commit();

            return _mapper.Map<StudentResultResponse>(entity);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation($"Deleting StudentResult id: {id}");
            await _studentResultRepo.DeleteAsync(id);
            await _unitOfWork.Commit();

            return true;
        }
    }
}