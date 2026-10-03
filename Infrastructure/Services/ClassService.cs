using System;
using Application.Abstractions;
using Application.Abstractions.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.Abstractions.Services;
using Application.ViewModels.Class;
using AutoMapper;
using Domain.Entities.Postgres.Scaffolded;
using Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Application.Common.Helpers.MemoryCache;

namespace Infrastructure.Services
{
    public class ClassService : IClassService
    {
        private readonly IClassRepository _classRepository;
        private readonly IMapper _mapper;
        private readonly IMemoryCacheHelper _cache;
        private readonly ILogger<ClassService> _logger;
        private readonly IUnitOfWork _unitOfWork;
        public ClassService(IClassRepository classRepository, IMapper mapper, IMemoryCacheHelper cache, ILogger<ClassService> logger, IUnitOfWork unitOfWork)
        {
            _classRepository = classRepository;
            _mapper = mapper;
            _cache = cache;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<ClassResponse>> GetByBranchIdAsync(Guid branchId)
        {
            var entities = await _classRepository.GetAllAsync(c => c.BranchId == branchId);
            return _mapper.Map<IEnumerable<ClassResponse>>(entities);
        }

        public async Task<IEnumerable<ClassResponse>> GetAllAsync()
        {
            const string cacheKey = "classes_all";
            var cached = _cache.GetCache<IEnumerable<ClassResponse>>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Fetched all classes from cache.");
                return cached;
            }
            var entities = await _classRepository.GetAllAsync();
            var dtos = _mapper.Map<IEnumerable<ClassResponse>>(entities);
            _cache.SetCache(cacheKey, dtos, TimeSpan.FromMinutes(10));
            _logger.LogInformation("Fetched all classes from DB and cached result.");
            return dtos;
        }
        public Task<ClassResponse?> GetByIdAsync(Guid id)
        {
            string cacheKey = $"class_{id}";
            var cached = _cache.GetCache<ClassResponse>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation($"Fetched class {id} from cache.");
                return Task.FromResult<ClassResponse?>(cached);
            }
            var entity = _classRepository.GetByIdAsync(id).Result;
            if (entity == null) return Task.FromResult<ClassResponse?>(null);
            var dto = _mapper.Map<ClassResponse>(entity);
            _cache.SetCache(cacheKey, dto, TimeSpan.FromMinutes(10));
            _logger.LogInformation($"Fetched class {id} from DB and cached result.");
            return Task.FromResult<ClassResponse?>(dto);
        }
        public async Task<ClassResponse> CreateAsync(ClassRequest input)
        {
            var entity = _mapper.Map<Class>(input);
            entity.Id = Guid.NewGuid();
            await _classRepository.AddAsync(entity);
            _cache.RemoveCache("classes_all");
            _logger.LogInformation($"Created new class {entity.Id}.");
            return _mapper.Map<ClassResponse>(entity);
        }
        public async Task<ClassResponse> UpdateAsync(Guid id, ClassRequest input)
        {
            var entity = await _classRepository.GetByIdAsync(id);
            if (entity == null) throw new Exception("Class not found");
            _mapper.Map(input, entity);
            await _classRepository.UpdateAsync(entity);
            _cache.RemoveCache("classes_all");
            _cache.RemoveCache($"class_{id}");
            _logger.LogInformation($"Updated class {id}.");
            return _mapper.Map<ClassResponse>(entity);
        }
        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _classRepository.GetByIdAsync(id);
            if (entity == null) throw new Exception("Class not found");
            await _classRepository.DeleteAsync(id);
            _cache.RemoveCache("classes_all");
            _cache.RemoveCache($"class_{id}");
            _logger.LogInformation($"Deleted class {id}.");
            return true;
        }
      
            public async Task<int> CommitAsync()
            {
                return await _unitOfWork.Commit();
            }
    }
}
