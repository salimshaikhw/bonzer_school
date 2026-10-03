using Application.Abstractions;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.ViewModels.Medium;
using AutoMapper;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class MediumService : IMediumService
    {
        private readonly IMediumRepository _mediumRepository;
        private readonly ILogger<MediumService> _logger;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public MediumService(
            IMediumRepository mediumRepository,
            ILogger<MediumService> logger,
            IMapper mapper,
            IUnitOfWork unitOfWork)
        {
            _mediumRepository = mediumRepository;
            _logger = logger;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<MediumResponse>> GetAllAsync()
        {
            _logger.LogInformation("Fetching all Mediums");
            var entities = await _mediumRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<MediumResponse>>(entities);
        }

        public async Task<MediumResponse> GetByIdAsync(Guid id)
        {
            _logger.LogInformation($"Fetching Medium by id: {id}");
            var entity = await _mediumRepository.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<MediumResponse>(entity);
        }

        public async Task<MediumResponse> CreateAsync(MediumRequest request)
        {
            _logger.LogInformation("Creating Medium");
            var entity = _mapper.Map<Medium>(request);

            await _mediumRepository.AddAsync(entity);
            await _unitOfWork.Commit();

            return _mapper.Map<MediumResponse>(entity);
        }

        public async Task<MediumResponse> UpdateAsync(Guid id, MediumRequest request)
        {
            _logger.LogInformation($"Updating Medium id: {id}");
            var entity = await _mediumRepository.GetByIdAsync(id);
            if (entity == null) return null;

            _mapper.Map(request, entity);
            await _mediumRepository.UpdateAsync(entity);
            await _unitOfWork.Commit();

            return _mapper.Map<MediumResponse>(entity);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation($"Deleting Medium id: {id}");
            await _mediumRepository.DeleteAsync(id);
            await _unitOfWork.Commit();

            return true;
        }
    }
}