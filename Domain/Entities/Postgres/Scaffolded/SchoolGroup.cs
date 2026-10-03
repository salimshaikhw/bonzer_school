using System;
using System.Collections.Generic;

namespace Domain.Entities.Postgres.Scaffolded;

public partial class SchoolGroup
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public string? Code { get; set; }

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public DateTime? CreatedAt { get; set; }
}
