using Application.Abstractions;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.ViewModels.StaffRole;
using AutoMapper;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class StaffRoleService : IStaffRoleService
    {
        private readonly ILogger<StaffRoleService> _logger;
        private readonly IStaffRoleRepository _staffRoleRepo;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public StaffRoleService(
            ILogger<StaffRoleService> logger,
            IStaffRoleRepository staffRoleRepo,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _logger = logger;
            _staffRoleRepo = staffRoleRepo;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<StaffRoleResponse>> GetAllAsync()
        {
            _logger.LogInformation("Fetching all StaffRoles");
            var entities = await _staffRoleRepo.GetAllAsync();
            return _mapper.Map<IEnumerable<StaffRoleResponse>>(entities);
        }

        public async Task<StaffRoleResponse> GetByIdAsync(Guid id)
        {
            _logger.LogInformation($"Fetching StaffRole by id: {id}");
            var entity = await _staffRoleRepo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<StaffRoleResponse>(entity);
        }

        public async Task<StaffRoleResponse> CreateAsync(StaffRoleRequest request)
        {
            _logger.LogInformation("Creating StaffRole");
            var entity = _mapper.Map<StaffRole>(request);

            await _staffRoleRepo.AddAsync(entity);
            await _unitOfWork.Commit();

            return _mapper.Map<StaffRoleResponse>(entity);
        }

        public async Task<StaffRoleResponse> UpdateAsync(Guid id, StaffRoleRequest request)
        {
            _logger.LogInformation($"Updating StaffRole id: {id}");
            var entity = await _staffRoleRepo.GetByIdAsync(id);
            if (entity == null) return null;

            _mapper.Map(request, entity);
            await _staffRoleRepo.UpdateAsync(entity);
            await _unitOfWork.Commit();

            return _mapper.Map<StaffRoleResponse>(entity);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation($"Deleting StaffRole id: {id}");
            await _staffRoleRepo.DeleteAsync(id);
            await _unitOfWork.Commit();

            return true;
        }
    }
}