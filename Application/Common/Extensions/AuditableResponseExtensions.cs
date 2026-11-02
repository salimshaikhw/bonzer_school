using Application.ViewModels.Common;
using Domain.Common;

namespace Application.Common.Extensions;

public static class AuditableResponseExtensions
{
    /// <summary>
    /// Copies Id and the audit fields from an entity onto a hand-built response,
    /// e.g. <c>new LeaveRequestDto { ... }.WithAuditFrom(entity)</c>. AutoMapper does this by itself.
    /// </summary>
    public static TResponse WithAuditFrom<TResponse>(this TResponse response, AuditableEntity entity)
        where TResponse : AuditableResponse
    {
        response.Id = entity.Id;
        response.CreatedAt = entity.CreatedAt;
        response.CreatedBy = entity.CreatedBy;
        response.UpdatedAt = entity.UpdatedAt;
        response.UpdatedBy = entity.UpdatedBy;
        return response;
    }
}
