using Application.Abstractions;
using Application.Abstractions.Services.Administration;
using Application.Common;
using Application.Exceptions;
using Application.ViewModels.AuditLog;
using AutoMapper;
using Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Administration
{
    /// <summary>
    /// The audit trail. Rules:
    ///   - append-only: entries can be written and read, never changed or deleted (otherwise the trail proves nothing);
    ///   - the time is set by the server, not taken from the request;
    ///   - lists return the newest <see cref="MaxListed"/> entries, newest first (the table only grows).
    /// </summary>
    public class AuditLogService : IAuditLogService
    {
        public const int MaxListed = 5000;

        private readonly BonzerSchoolDbContext _db;
        private readonly IMapper _mapper;
        private readonly ILogger<AuditLogService> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly TimeProvider _clock;

        public AuditLogService(BonzerSchoolDbContext db, IMapper mapper, ILogger<AuditLogService> logger, IUnitOfWork unitOfWork, TimeProvider clock)
        {
            _db = db;
            _mapper = mapper;
            _logger = logger;
            _unitOfWork = unitOfWork;
            _clock = clock;
        }

        public async Task<IEnumerable<AuditLogResponse>> GetAllAsync()
        {
            var entries = await _db.AuditLogs.AsNoTracking()
                .OrderByDescending(a => a.CreatedAt)
                .Take(MaxListed)
                .ToListAsync();
            return _mapper.Map<IEnumerable<AuditLogResponse>>(entries);
        }

        public async Task<AuditLogResponse?> GetByIdAsync(Guid id)
        {
            var entry = await _db.AuditLogs.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id)
                        ?? throw new BaseServiceException($"Audit log {id} not found.", ExceptionCodes.ItemNotFound);
            return _mapper.Map<AuditLogResponse>(entry);
        }

        public async Task<AuditLogResponse> CreateAsync(AuditLogRequest input)
        {
            var entry = _mapper.Map<AuditLog>(input);
            entry.Id = Guid.NewGuid();
            entry.CreatedAt = _clock.GetUtcNow().UtcDateTime;
            _db.AuditLogs.Add(entry);
            await _db.SaveChangesAsync(); // was never saved before: the controller does not commit
            _logger.LogInformation("Audit entry {AuditId}: {Module} {ActionType} on {RecordId}", entry.Id, entry.Module, entry.ActionType, entry.RecordId);
            return _mapper.Map<AuditLogResponse>(entry);
        }

        public Task<AuditLogResponse> UpdateAsync(Guid id, AuditLogRequest input) =>
            throw new BaseServiceException("Audit log entries cannot be changed.", ExceptionCodes.Operation);

        public Task<bool> DeleteAsync(Guid id) =>
            throw new BaseServiceException("Audit log entries cannot be deleted.", ExceptionCodes.Operation);

        public Task<int> CommitAsync() => _unitOfWork.Commit();
    }
}
