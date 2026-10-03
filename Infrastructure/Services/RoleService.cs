using Application.Abstractions;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.ViewModels.Role;
using AutoMapper;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class RoleService : IRoleService
    {
        private readonly ILogger<RoleService> _logger;
        private readonly IRoleRepository _roleRepo;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public RoleService(
            ILogger<RoleService> logger,
            IRoleRepository roleRepo,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _logger = logger;
            _roleRepo = roleRepo;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<RoleResponse>> GetAllAsync()
        {
            _logger.LogInformation("Fetching all Roles");
            var entities = await _roleRepo.GetAllAsync();
            return _mapper.Map<IEnumerable<RoleResponse>>(entities);
        }

        public async Task<RoleResponse> GetByIdAsync(Guid id)
        {
            _logger.LogInformation($"Fetching Role by id: {id}");
            var entity = await _roleRepo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<RoleResponse>(entity);
        }

        public async Task<RoleResponse> CreateAsync(RoleRequest request)
        {
            _logger.LogInformation("Creating Role");
            var entity = _mapper.Map<Role>(request);

            await _roleRepo.AddAsync(entity);
            await _unitOfWork.Commit();

            return _mapper.Map<RoleResponse>(entity);
        }

        public async Task<RoleResponse> UpdateAsync(Guid id, RoleRequest request)
        {
            _logger.LogInformation($"Updating Role id: {id}");
            var entity = await _roleRepo.GetByIdAsync(id);
            if (entity == null) return null;

            _mapper.Map(request, entity);
            await _roleRepo.UpdateAsync(entity);
            await _unitOfWork.Commit();

            return _mapper.Map<RoleResponse>(entity);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation($"Deleting Role id: {id}");
            await _roleRepo.DeleteAsync(id);
            await _unitOfWork.Commit();

            return true;
        }
    }
}