using System;

namespace Application.ViewModels.AuditLog
{
    public class AuditLogResponse
    {
        public Guid Id { get; set; }
        public Guid? BranchId { get; set; }
        public Guid? UserId { get; set; }
        public string? Module { get; set; }
        public string? ActionType { get; set; }
        public Guid? RecordId { get; set; }
        public string? Description { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
