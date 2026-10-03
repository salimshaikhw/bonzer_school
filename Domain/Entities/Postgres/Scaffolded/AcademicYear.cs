using System;
using System.Collections.Generic;

namespace Infrastructure;

public partial class AcademicYear
{
    public Guid Id { get; set; }

    public string? YearName { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool? IsActive { get; set; }
}
