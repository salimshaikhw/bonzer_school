using Application.Abstractions;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.ViewModels.Division;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class DivisionService : IDivisionService
    {
        private readonly IDivisionRepository _divisionRepository;
        private readonly ILogger<DivisionService> _logger;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        public DivisionService(
            IDivisionRepository divisionRepository,
            ILogger<DivisionService> logger,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            ICacheService cacheService)
        {
            _divisionRepository = divisionRepository;
            _logger = logger;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
        }

        public async Task<IEnumerable<DivisionResponse>> GetAllAsync()
        {
            _logger.LogInformation("Fetching all Divisions");
            var entities = await _divisionRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<DivisionResponse>>(entities);
        }

        public async Task<IEnumerable<DivisionResponse>> GetByClassIdAsync(Guid classId)
        {
            _logger.LogInformation("Fetching Divisions for ClassId {ClassId}", classId);
            var entities = await _divisionRepository.GetAllAsync(d => d.ClassId == classId);
            return _mapper.Map<IEnumerable<DivisionResponse>>(entities);
        }

        public async Task<DivisionResponse> GetByIdAsync(Guid id)
        {
            _logger.LogInformation($"Fetching Division by id: {id}");
            var entity = await _divisionRepository.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<DivisionResponse>(entity);
        }

        public async Task<DivisionResponse> CreateAsync(DivisionRequest request)
        {
            _logger.LogInformation("Creating Division");
            var entity = _mapper.Map<Division>(request);

            await _divisionRepository.AddAsync(entity);
            await _unitOfWork.Commit();

            _cacheService.Remove("Divisions:all");

            return _mapper.Map<DivisionResponse>(entity);
        }

        public async Task<DivisionResponse> UpdateAsync(Guid id, DivisionRequest request)
        {
            _logger.LogInformation($"Updating Division id: {id}");
            var entity = await _divisionRepository.GetByIdAsync(id);
            if (entity == null) return null;

            _mapper.Map(request, entity);
            await _divisionRepository.UpdateAsync(entity);
            await _unitOfWork.Commit();

            _cacheService.Remove("Divisions:all");
            _cacheService.Remove($"Division:{id}");

            return _mapper.Map<DivisionResponse>(entity);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation($"Deleting Division id: {id}");
            await _divisionRepository.DeleteAsync(id);
            await _unitOfWork.Commit();

            _cacheService.Remove("Divisions:all");
            _cacheService.Remove($"Division:{id}");

            return true;
        }
    }
}