using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.ViewModels.AuditLog;

namespace Application.Abstractions.Services
{
    public interface IAuditLogService
    {
        Task<IEnumerable<AuditLogResponse>> GetAllAsync();
        Task<AuditLogResponse?> GetByIdAsync(Guid id);
        Task<AuditLogResponse> CreateAsync(AuditLogRequest request);
        Task<AuditLogResponse> UpdateAsync(Guid id, AuditLogRequest request);
        Task<bool> DeleteAsync(Guid id);
        Task<int> CommitAsync();
    }
}
