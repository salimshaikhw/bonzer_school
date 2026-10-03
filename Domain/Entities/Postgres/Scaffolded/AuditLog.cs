using System;
using System.Collections.Generic;

namespace Infrastructure;

public partial class AuditLog
{
    public Guid id { get; set; }

    public Guid? branch_id { get; set; }

    public Guid? user_id { get; set; }

    public string? module { get; set; }

    public string? action_type { get; set; }

    public Guid? record_id { get; set; }

    public string? description { get; set; }

    public DateTime? created_at { get; set; }
}
