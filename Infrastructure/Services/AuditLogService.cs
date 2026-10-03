using System;
using Application.Abstractions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.Abstractions.Services;
using Application.ViewModels.AuditLog;
using AutoMapper;
using Domain.Entities.Postgres.Scaffolded;
using Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Application.Common.Helpers.MemoryCache;

namespace Infrastructure.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly BonzerSchoolDbContext _db;
        private readonly IMapper _mapper;
        private readonly IMemoryCacheHelper _cache;
        private readonly ILogger<AuditLogService> _logger;
        private readonly IUnitOfWork _unitOfWork;
        public AuditLogService(BonzerSchoolDbContext db, IMapper mapper, IMemoryCacheHelper cache, ILogger<AuditLogService> logger, IUnitOfWork unitOfWork)
        {
            _db = db;
            _mapper = mapper;
            _cache = cache;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }
        public async Task<IEnumerable<AuditLogResponse>> GetAllAsync()
        {
            const string cacheKey = "audit_logs_all";
            var cached = _cache.GetCache<IEnumerable<AuditLogResponse>>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Fetched all audit logs from cache.");
                return cached;
            }
            var entities = await _db.audit_logs.ToListAsync();
            var dtos = _mapper.Map<IEnumerable<AuditLogResponse>>(entities);
            _cache.SetCache(cacheKey, dtos, TimeSpan.FromMinutes(10));
            _logger.LogInformation("Fetched all audit logs from DB and cached result.");
            return dtos;
        }
        public Task<AuditLogResponse?> GetByIdAsync(Guid id)
        {
            string cacheKey = $"audit_log_{id}";
            var cached = _cache.GetCache<AuditLogResponse>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation($"Fetched audit log {id} from cache.");
                return Task.FromResult<AuditLogResponse?>(cached);
            }
            var entity = _db.audit_logs.Find(id);
            if (entity == null) return Task.FromResult<AuditLogResponse?>(null);
            var dto = _mapper.Map<AuditLogResponse>(entity);
            _cache.SetCache(cacheKey, dto, TimeSpan.FromMinutes(10));
            _logger.LogInformation($"Fetched audit log {id} from DB and cached result.");
            return Task.FromResult<AuditLogResponse?>(dto);
        }
        public async Task<AuditLogResponse> CreateAsync(AuditLogRequest input)
        {
            var entity = _mapper.Map<AuditLog>(input);
            entity.id = Guid.NewGuid();
            _db.audit_logs.Add(entity);
            // Do not commit here; commit should be called after all changes are staged if batching
            _cache.RemoveCache("audit_logs_all");
            _logger.LogInformation($"Created new audit log {entity.id}.");
            return _mapper.Map<AuditLogResponse>(entity);
        }
        public async Task<AuditLogResponse> UpdateAsync(Guid id, AuditLogRequest input)
        {
            var entity = await _db.audit_logs.FindAsync(id);
            if (entity == null) throw new Exception("Audit log not found");
            _mapper.Map(input, entity);
            // Do not commit here; commit should be called after all changes are staged if batching
            _cache.RemoveCache("audit_logs_all");
            _cache.RemoveCache($"audit_log_{id}");
            _logger.LogInformation($"Updated audit log {id}.");
            return _mapper.Map<AuditLogResponse>(entity);
        }
        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _db.audit_logs.FindAsync(id);
            if (entity == null) throw new Exception("Audit log not found");
            _db.audit_logs.Remove(entity);
            // Do not commit here; commit should be called after all changes are staged if batching
            _cache.RemoveCache("audit_logs_all");
            _cache.RemoveCache($"audit_log_{id}");
            _logger.LogInformation($"Deleted audit log {id}.");
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
