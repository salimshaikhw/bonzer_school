using System;
using System.Collections.Generic;

namespace Infrastructure;

public partial class FeeHead
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public string? AccountCode { get; set; }
}
