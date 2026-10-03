namespace Application.ViewModels.Division;

public class DivisionRequest
{
    public Guid? ClassId { get; set; }
    public Guid? BranchId { get; set; }
    public string? Name { get; set; }
    public string? Code { get; set; }
    public int? AvailableSeats { get; set; }
    public DateTime? CreatedAt { get; set; }
}
