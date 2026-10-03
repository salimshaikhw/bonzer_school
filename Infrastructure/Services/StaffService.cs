using Application.Abstractions;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.ViewModels.Staff;
using AutoMapper;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class StaffService : IStaffService
    {
        private readonly ILogger<StaffService> _logger;
        private readonly IStaffRepository _staffRepo;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public StaffService(
            ILogger<StaffService> logger,
            IStaffRepository staffRepo,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _logger = logger;
            _staffRepo = staffRepo;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<StaffResponse>> GetAllAsync()
        {
            _logger.LogInformation("Fetching all Staff members");
            var entities = await _staffRepo.GetAllAsync();
            return _mapper.Map<IEnumerable<StaffResponse>>(entities);
        }

        public async Task<StaffResponse> GetByIdAsync(Guid id)
        {
            _logger.LogInformation($"Fetching Staff by id: {id}");
            var entity = await _staffRepo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<StaffResponse>(entity);
        }

        public async Task<StaffResponse> CreateAsync(StaffRequest request)
        {
            _logger.LogInformation("Creating Staff");
            var entity = _mapper.Map<Staff>(request);

            await _staffRepo.AddAsync(entity);
            await _unitOfWork.Commit();

            return _mapper.Map<StaffResponse>(entity);
        }

        public async Task<StaffResponse> UpdateAsync(Guid id, StaffRequest request)
        {
            _logger.LogInformation($"Updating Staff id: {id}");
            var entity = await _staffRepo.GetByIdAsync(id);
            if (entity == null) return null;

            _mapper.Map(request, entity);
            await _staffRepo.UpdateAsync(entity);
            await _unitOfWork.Commit();

            return _mapper.Map<StaffResponse>(entity);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation($"Deleting Staff id: {id}");
            await _staffRepo.DeleteAsync(id);
            await _unitOfWork.Commit();

            return true;
        }
    }
}