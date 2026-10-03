namespace Application.ViewModels.FeeStructure
{
    public class FeeStructureResponse
    {
        public Guid Id { get; set; }
        public Guid? ClassId { get; set; }
        public Guid? DivisionId { get; set; }
        public Guid? FeeHeadId { get; set; }
        public decimal? Amount { get; set; }
    }
}
