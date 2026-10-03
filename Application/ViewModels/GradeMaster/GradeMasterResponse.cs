namespace Application.ViewModels.GradeMaster
{
    public class GradeMasterResponse
    {
        public Guid Id { get; set; }
        public string? Grade { get; set; }
        public decimal? MinPercent { get; set; }
        public decimal? MaxPercent { get; set; }
    }
}
