using System;
using System.Collections.Generic;

namespace Infrastructure;

public partial class ExamType
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }
}
