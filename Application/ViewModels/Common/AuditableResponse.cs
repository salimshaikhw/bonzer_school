namespace Application.ViewModels.Common;

/// <summary>
/// Response counterpart of Domain.Common.AuditableEntity: who created / last changed the row, and when.
/// AutoMapper fills these by name. Request DTOs never carry them: the server sets them on save.
/// </summary>
public abstract class AuditableResponse : BaseResponse
{
    public DateTime? CreatedAt { get; set; }

    /// <summary>ASP.NET Identity user id of the creator.</summary>
    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    /// <summary>ASP.NET Identity user id of the last editor.</summary>
    public string? UpdatedBy { get; set; }
}
