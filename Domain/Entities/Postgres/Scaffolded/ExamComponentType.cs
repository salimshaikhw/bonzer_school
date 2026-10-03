using System;
using System.Collections.Generic;

namespace Infrastructure;

public partial class ExamComponentType
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Code { get; set; }

    public DateTime? CreatedAt { get; set; }
}
