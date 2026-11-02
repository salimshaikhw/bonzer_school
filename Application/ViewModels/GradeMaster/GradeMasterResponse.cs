namespace Application.ViewModels.GradeMaster
{
    public class GradeMasterResponse : AuditableResponse
    {
        public string? Grade { get; set; }
        public decimal? MinPercent { get; set; }
        public decimal? MaxPercent { get; set; }
    }
}
