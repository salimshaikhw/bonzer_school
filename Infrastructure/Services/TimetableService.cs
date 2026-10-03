using Application.Abstractions;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.ViewModels.Timetable;
using AutoMapper;
using Domain.Entities;
using Domain.Entities.Postgres.Scaffolded;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class TimetableService : ITimetableService
    {
        private readonly ILogger<TimetableService> _logger;
        private readonly ITimetableRepository _timetableRepo;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public TimetableService(
            ILogger<TimetableService> logger,
            ITimetableRepository timetableRepo,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _logger = logger;
            _timetableRepo = timetableRepo;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<TimetableResponse>> GetAllAsync()
        {
            _logger.LogInformation("Fetching all Timetables");
            var entities = await _timetableRepo.GetAllAsync();
            return _mapper.Map<IEnumerable<TimetableResponse>>(entities);
        }

        public async Task<TimetableResponse> GetByIdAsync(Guid id)
        {
            _logger.LogInformation($"Fetching Timetable by id: {id}");
            var entity = await _timetableRepo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<TimetableResponse>(entity);
        }

        public async Task<TimetableResponse> CreateAsync(TimetableRequest request)
        {
            _logger.LogInformation("Creating Timetable");
            var entity = _mapper.Map<Timetable>(request);

            await _timetableRepo.AddAsync(entity);
            await _unitOfWork.Commit();

            return _mapper.Map<TimetableResponse>(entity);
        }

        public async Task<TimetableResponse> UpdateAsync(Guid id, TimetableRequest request)
        {
            _logger.LogInformation($"Updating Timetable id: {id}");
            var entity = await _timetableRepo.GetByIdAsync(id);
            if (entity == null) return null;

            _mapper.Map(request, entity);
            await _timetableRepo.UpdateAsync(entity);
            await _unitOfWork.Commit();

            return _mapper.Map<TimetableResponse>(entity);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation($"Deleting Timetable id: {id}");
            await _timetableRepo.DeleteAsync(id);
            await _unitOfWork.Commit();

            return true;
        }
    }
}