using Application.Abstractions;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.ViewModels.ClassTeacherMap;
using AutoMapper;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class ClassTeacherMapService : IClassTeacherMapService
    {
        private readonly ILogger<ClassTeacherMapService> _logger;
        private readonly IClassTeacherMapRepository _classTeacherRepo;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public ClassTeacherMapService(
            ILogger<ClassTeacherMapService> logger,
            IClassTeacherMapRepository classTeacherRepo,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _logger = logger;
            _classTeacherRepo = classTeacherRepo;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ClassTeacherMapResponse>> GetAllAsync()
        {
            _logger.LogInformation("Fetching all ClassTeacherMaps");
            var entities = await _classTeacherRepo.GetAllAsync();
            return _mapper.Map<IEnumerable<ClassTeacherMapResponse>>(entities);
        }

        public async Task<ClassTeacherMapResponse> GetByIdAsync(Guid id)
        {
            _logger.LogInformation($"Fetching ClassTeacherMap by id: {id}");
            var entity = await _classTeacherRepo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<ClassTeacherMapResponse>(entity);
        }

        public async Task<ClassTeacherMapResponse> CreateAsync(ClassTeacherMapRequest request)
        {
            _logger.LogInformation("Creating ClassTeacherMap");
            var entity = _mapper.Map<ClassTeacherMap>(request);

            await _classTeacherRepo.AddAsync(entity);
            await _unitOfWork.Commit();

            return _mapper.Map<ClassTeacherMapResponse>(entity);
        }

        public async Task<ClassTeacherMapResponse> UpdateAsync(Guid id, ClassTeacherMapRequest request)
        {
            _logger.LogInformation($"Updating ClassTeacherMap id: {id}");
            var entity = await _classTeacherRepo.GetByIdAsync(id);
            if (entity == null) return null;

            _mapper.Map(request, entity);
            await _classTeacherRepo.UpdateAsync(entity);
            await _unitOfWork.Commit();

            return _mapper.Map<ClassTeacherMapResponse>(entity);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation($"Deleting ClassTeacherMap id: {id}");
            await _classTeacherRepo.DeleteAsync(id);
            await _unitOfWork.Commit();

            return true;
        }
    }
}
