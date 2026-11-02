namespace Application.ViewModels.FeeHead
{
    public class FeeHeadResponse : AuditableResponse
    {
        public string? Name { get; set; }
        public string? AccountCode { get; set; }
    }
}
