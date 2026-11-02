using Application.ViewModels.AuditLog;
using AutoMapper;

namespace Infrastructure.Mapping
{
    public class AuditLogProfile : Profile
    {
        public AuditLogProfile()
        {
            CreateMap<AuditLog, AuditLogResponse>();

            CreateMap<AuditLogRequest, AuditLog>();
        }
    }
}
