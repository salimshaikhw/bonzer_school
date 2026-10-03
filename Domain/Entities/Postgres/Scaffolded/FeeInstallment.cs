using System;
using System.Collections.Generic;

namespace Infrastructure;

public partial class FeeInstallment
{
    public Guid Id { get; set; }

    public Guid? BranchId { get; set; }

    public Guid? ClassId { get; set; }

    public Guid? AcademicYearId { get; set; }

    public int? InstallmentNo { get; set; }

    public string? InstallmentName { get; set; }

    public DateOnly? DueDate { get; set; }

    public DateOnly? LateFeeAfter { get; set; }
    public int? GraceDays { get; set; }      
    public decimal? LateFeePerDay { get; set; } 

    public DateTime? CreatedAt { get; set; }
}
