using System;
using System.Collections.Generic;

namespace Infrastructure;

public partial class Notification
{
    public Guid Id { get; set; }

    public Guid? StudentId { get; set; }

    public Guid? BranchId { get; set; }

    public Guid? DivisionId { get; set; }

    public string? Type { get; set; }

    public string? Title { get; set; }

    public string? Message { get; set; }

    public DateTime? SentAt { get; set; }
}
