namespace Application.ViewModels.GradeMaster
{
    public class GradeMasterRequest
    {
        public string? Grade { get; set; }
        public decimal? MinPercent { get; set; }
        public decimal? MaxPercent { get; set; }
    }
}
