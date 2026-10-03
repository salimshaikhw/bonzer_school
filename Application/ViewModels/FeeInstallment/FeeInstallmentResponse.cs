namespace Application.ViewModels.FeeInstallment
{
    public class FeeInstallmentResponse
    {
        public Guid Id { get; set; }
        public Guid? BranchId { get; set; }
        public Guid? ClassId { get; set; }
        public Guid? AcademicYearId { get; set; }
        public int? InstallmentNo { get; set; }
        public string? InstallmentName { get; set; }
        public DateOnly? DueDate { get; set; }
        public DateOnly? LateFeeAfter { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
