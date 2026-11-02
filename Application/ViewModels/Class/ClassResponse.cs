namespace Application.ViewModels.Class;

public class ClassResponse : AuditableResponse
{
    public Guid? BranchId { get; set; }
    public string? Name { get; set; }
}
