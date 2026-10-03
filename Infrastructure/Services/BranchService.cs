using Application.Abstractions.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.Abstractions.Services;
using Application.Abstractions;
using Application.ViewModels.Branch;
using AutoMapper;
using Domain.Entities.Postgres.Scaffolded;
using Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Application.Common.Helpers.MemoryCache;

namespace Infrastructure.Services
{
    public class BranchService : IBranchService
    {
        private readonly IBranchRepository _branchRepository;
        private readonly IMapper _mapper;
        private readonly IMemoryCacheHelper _cache;
        private readonly ILogger<BranchService> _logger;
        private readonly IUnitOfWork _unitOfWork;
        public BranchService(IBranchRepository branchRepository, IMapper mapper, IMemoryCacheHelper cache, ILogger<BranchService> logger, IUnitOfWork unitOfWork)
        {
            _branchRepository = branchRepository;
            _mapper = mapper;
            _cache = cache;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }
        public async Task<IEnumerable<BranchResponse>> GetAllAsync()
        {
            const string cacheKey = "branches_all";
            var cached = _cache.GetCache<IEnumerable<BranchResponse>>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Fetched all branches from cache.");
                return cached;
            }
            var entities = await _branchRepository.GetAllAsync();
            var dtos = _mapper.Map<IEnumerable<BranchResponse>>(entities);
            _cache.SetCache(cacheKey, dtos, TimeSpan.FromMinutes(10));
            _logger.LogInformation("Fetched all branches from DB and cached result.");
            return dtos;
        }
        public async Task<BranchResponse?> GetByIdAsync(Guid id)
        {
            string cacheKey = $"branch_{id}";
            var cached = _cache.GetCache<BranchResponse>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation($"Fetched branch {id} from cache.");
                return cached;
            }
            var entity = await _branchRepository.GetByIdAsync(id);
            if (entity == null) return null;
            var dto = _mapper.Map<BranchResponse>(entity);
            _cache.SetCache(cacheKey, dto, TimeSpan.FromMinutes(10));
            _logger.LogInformation($"Fetched branch {id} from DB and cached result.");
            return dto;
        }
        public async Task<BranchResponse> CreateAsync(BranchRequest input)
        {
            var entity = _mapper.Map<Branch>(input);
            entity.Id = Guid.NewGuid();
            await _branchRepository.AddAsync(entity);
            _cache.RemoveCache("branches_all");
            _logger.LogInformation($"Created new branch {entity.Id}.");
            return _mapper.Map<BranchResponse>(entity);
        }
        public async Task<BranchResponse> UpdateAsync(Guid id, BranchRequest input)
        {
            var entity = await _branchRepository.GetByIdAsync(id);
            if (entity == null) throw new Exception("Branch not found");
            _mapper.Map(input, entity);
            await _branchRepository.UpdateAsync(entity);
            _cache.RemoveCache("branches_all");
            _cache.RemoveCache($"branch_{id}");
            _logger.LogInformation($"Updated branch {id}.");
            return _mapper.Map<BranchResponse>(entity);
        }
        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _branchRepository.GetByIdAsync(id);
            if (entity == null) throw new Exception("Branch not found");
            await _branchRepository.DeleteAsync(id);
            _cache.RemoveCache("branches_all");
            _cache.RemoveCache($"branch_{id}");
            _logger.LogInformation($"Deleted branch {id}.");
            return true;
        }
            /// <summary>
            /// Call this method after all create/update/delete operations to persist changes as a single transaction.
            /// </summary>
            public async Task<int> CommitAsync()
            {
                return await _unitOfWork.Commit();
            }
    }
}
