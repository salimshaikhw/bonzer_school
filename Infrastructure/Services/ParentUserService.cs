using Application.Abstractions;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.ViewModels.ParentUser;
using AutoMapper;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class ParentUserService : IParentUserService
    {
        private readonly ILogger<ParentUserService> _logger;
        private readonly IParentUserRepository _parentUserRepo;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public ParentUserService(
            ILogger<ParentUserService> logger,
            IParentUserRepository parentUserRepo,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _logger = logger;
            _parentUserRepo = parentUserRepo;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ParentUserResponse>> GetAllAsync()
        {
            _logger.LogInformation("Fetching all ParentUsers");
            var entities = await _parentUserRepo.GetAllAsync();
            return _mapper.Map<IEnumerable<ParentUserResponse>>(entities);
        }

        public async Task<ParentUserResponse> GetByIdAsync(Guid id)
        {
            _logger.LogInformation($"Fetching ParentUser by id: {id}");
            var entity = await _parentUserRepo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<ParentUserResponse>(entity);
        }

        public async Task<ParentUserResponse> CreateAsync(ParentUserRequest request)
        {
            _logger.LogInformation("Creating ParentUser");
            var entity = _mapper.Map<ParentUser>(request);

            await _parentUserRepo.AddAsync(entity);
            await _unitOfWork.Commit();

            return _mapper.Map<ParentUserResponse>(entity);
        }

        public async Task<ParentUserResponse> UpdateAsync(Guid id, ParentUserRequest request)
        {
            _logger.LogInformation($"Updating ParentUser id: {id}");
            var entity = await _parentUserRepo.GetByIdAsync(id);
            if (entity == null) return null;

            _mapper.Map(request, entity);
            await _parentUserRepo.UpdateAsync(entity);
            await _unitOfWork.Commit();

            return _mapper.Map<ParentUserResponse>(entity);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation($"Deleting ParentUser id: {id}");
            await _parentUserRepo.DeleteAsync(id);
            await _unitOfWork.Commit();

            return true;
        }
    }
}