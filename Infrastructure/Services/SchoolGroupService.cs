using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Helpers.MemoryCache;
using Application.Exceptions;
using Application.ViewModels.SchoolGroup;
using Domain.Entities.Postgres.Scaffolded;
using Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class SchoolGroupService : ISchoolGroupService
    {
        private readonly ISchoolGroupRepository _schoolGroupRepository;
        private readonly AutoMapper.IMapper _mapper;
        private readonly IMemoryCacheHelper _cache;
        private readonly ILogger<SchoolGroupService> _logger;
        public SchoolGroupService(ISchoolGroupRepository schoolGroupRepository, AutoMapper.IMapper mapper, IMemoryCacheHelper cache, ILogger<SchoolGroupService> logger)
        {
            _schoolGroupRepository = schoolGroupRepository;
            _mapper = mapper;
            _cache = cache;
            _logger = logger;
        }

        public async Task<IEnumerable<SchoolGroupDto>> GetAllAsync()
        {
            const string cacheKey = "school_groups_all";
            var cached = _cache.GetCache<IEnumerable<SchoolGroupDto>>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Fetched all school groups from cache.");
                return cached;
            }
            var entities = await _schoolGroupRepository.GetAllAsync();
            var dtos = _mapper.Map<IEnumerable<SchoolGroupDto>>(entities);
            _cache.SetCache(cacheKey, dtos, TimeSpan.FromMinutes(10));
            _logger.LogInformation("Fetched all school groups from DB and cached result.");
            return dtos;
        }

        public async Task<SchoolGroupDto?> GetByIdAsync(Guid id)
        {
            string cacheKey = $"school_group_{id}";
            var cached = _cache.GetCache<SchoolGroupDto>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation($"Fetched school group {id} from cache.");
                return cached;
            }
            var entity = await _schoolGroupRepository.GetByIdAsync(id);
            if (entity == null) return null;
            var dto = _mapper.Map<SchoolGroupDto>(entity);
            _cache.SetCache(cacheKey, dto, TimeSpan.FromMinutes(10));
            _logger.LogInformation($"Fetched school group {id} from DB and cached result.");
            return dto;
        }

        public async Task<SchoolGroupDto> CreateAsync(CreateSchoolGroupInput input)
        {
            var entity = _mapper.Map<SchoolGroup>(input);
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;
            await _schoolGroupRepository.AddAsync(entity);
            _cache.RemoveCache("school_groups_all");
            _logger.LogInformation($"Created new school group {entity.Id}.");
            return _mapper.Map<SchoolGroupDto>(entity);
        }

        //public async Task<SchoolGroupDto> UpdateAsync(Guid id,UpdateSchoolGroupInput input)
        //{
        //    var entity = await _schoolGroupRepository.GetByIdAsync(input.Id);
        //    if (entity == null) throw new Exception("School group not found");
        //    _mapper.Map(input, entity);
        //    await _schoolGroupRepository.UpdateAsync(entity);
        //    _cache.RemoveCache("school_groups_all");
        //    _cache.RemoveCache($"school_group_{input.Id}");
        //    _logger.LogInformation($"Updated school group {input.Id}.");
        //    return _mapper.Map<SchoolGroupDto>(entity);
        //}


        public async Task<SchoolGroupDto> UpdateAsync(Guid id, UpdateSchoolGroupInput input)
        {
            var entity = await _schoolGroupRepository.GetByIdAsync(id);
            if (entity == null)
                throw new Exception("School group not found");

            _mapper.Map(input, entity);
            await _schoolGroupRepository.UpdateAsync(entity);

            _cache.RemoveCache("school_groups_all");
            _cache.RemoveCache($"school_group_{id}");

            _logger.LogInformation($"Updated school group {id}.");
            return _mapper.Map<SchoolGroupDto>(entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await _schoolGroupRepository.GetByIdAsync(id);
            if (entity == null) throw new Exception("School group not found");
            await _schoolGroupRepository.DeleteAsync(id);
            _cache.RemoveCache("school_groups_all");
            _cache.RemoveCache($"school_group_{id}");
            _logger.LogInformation($"Deleted school group {id}.");
        }
    }
}
