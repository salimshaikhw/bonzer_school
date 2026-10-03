using Application.Abstractions.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.Abstractions.Services;
using Application.ViewModels.AcademicYear;
using AutoMapper;
using Domain.Entities.Postgres.Scaffolded;
using Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Application.Common.Helpers.MemoryCache;

namespace Infrastructure.Services
{
    public class AcademicYearService : IAcademicYearService
    {
        private readonly IAcademicYearRepository _academicYearRepository;
        private readonly IMapper _mapper;
        private readonly IMemoryCacheHelper _cache;
        private readonly ILogger<AcademicYearService> _logger;
        public AcademicYearService(IAcademicYearRepository academicYearRepository, IMapper mapper, IMemoryCacheHelper cache, ILogger<AcademicYearService> logger)
        {
            _academicYearRepository = academicYearRepository;
            _mapper = mapper;
            _cache = cache;
            _logger = logger;
        }
        public async Task<IEnumerable<AcademicYearResponse>> GetAllAsync()
        {
            const string cacheKey = "academic_years_all";
            var cached = _cache.GetCache<IEnumerable<AcademicYearResponse>>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Fetched all academic years from cache.");
                return cached;
            }
            var entities = await _academicYearRepository.GetAllAsync();
            var dtos = _mapper.Map<IEnumerable<AcademicYearResponse>>(entities);
            _cache.SetCache(cacheKey, dtos, TimeSpan.FromMinutes(10));
            _logger.LogInformation("Fetched all academic years from DB and cached result.");
            return dtos;
        }
        public async Task<AcademicYearResponse?> GetByIdAsync(Guid id)
        {
            string cacheKey = $"academic_year_{id}";
            var cached = _cache.GetCache<AcademicYearResponse>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation($"Fetched academic year {id} from cache.");
                return cached;
            }
            var entity = await _academicYearRepository.GetByIdAsync(id);
            if (entity == null) return null;
            var dto = _mapper.Map<AcademicYearResponse>(entity);
            _cache.SetCache(cacheKey, dto, TimeSpan.FromMinutes(10));
            _logger.LogInformation($"Fetched academic year {id} from DB and cached result.");
            return dto;
        }
        public async Task<AcademicYearResponse> CreateAsync(AcademicYearRequest input)
        {
            var entity = _mapper.Map<AcademicYear>(input);
            entity.Id = Guid.NewGuid();
            await _academicYearRepository.AddAsync(entity);
            _cache.RemoveCache("academic_years_all");
            _logger.LogInformation($"Created new academic year {entity.Id}.");
            return _mapper.Map<AcademicYearResponse>(entity);
        }
        public async Task<AcademicYearResponse> UpdateAsync(Guid id, AcademicYearRequest input)
        {
            var entity = await _academicYearRepository.GetByIdAsync(id);
            if (entity == null) throw new Exception("Academic year not found");
            _mapper.Map(input, entity);
            await _academicYearRepository.UpdateAsync(entity);
            _cache.RemoveCache("academic_years_all");
            _cache.RemoveCache($"academic_year_{id}");
            _logger.LogInformation($"Updated academic year {id}.");
            return _mapper.Map<AcademicYearResponse>(entity);
        }
        public async Task DeleteAsync(Guid id)
        {
            var entity = await _academicYearRepository.GetByIdAsync(id);
            if (entity == null) throw new Exception("Academic year not found");
            await _academicYearRepository.DeleteAsync(id);
            _cache.RemoveCache("academic_years_all");
            _cache.RemoveCache($"academic_year_{id}");
            _logger.LogInformation($"Deleted academic year {id}.");
        }
    }
}
