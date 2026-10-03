namespace Application.ViewModels.FeeStructure
{
    public class FeeStructureRequest
    {
        public Guid? ClassId { get; set; }
        public Guid? DivisionId { get; set; }
        public Guid? FeeHeadId { get; set; }
        public decimal? Amount { get; set; }
    }
}
