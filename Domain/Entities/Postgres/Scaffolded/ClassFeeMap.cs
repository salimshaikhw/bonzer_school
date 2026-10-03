using System;
using System.Collections.Generic;

namespace Infrastructure;

public partial class ClassFeeMap
{
    public Guid Id { get; set; }

    public Guid? ClassId { get; set; }

    public Guid? DivisionId { get; set; }

    public Guid? AcademicYearId { get; set; }

    public Guid? FeeHeadId { get; set; }

    public decimal? Amount { get; set; }

    public int? InstallmentNo { get; set; }

    public string? AcademicYear { get; set; }

    public DateTime? CreatedAt { get; set; }
}
