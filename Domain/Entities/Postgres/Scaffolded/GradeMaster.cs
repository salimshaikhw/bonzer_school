using System;
using System.Collections.Generic;

namespace Infrastructure;

public partial class GradeMaster
{
    public Guid Id { get; set; }

    public string? Grade { get; set; }

    public decimal? MinPercent { get; set; }

    public decimal? MaxPercent { get; set; }

}
