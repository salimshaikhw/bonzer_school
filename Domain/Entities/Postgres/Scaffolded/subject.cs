using System;
using System.Collections.Generic;

namespace Domain.Entities.Postgres.Scaffolded;

public partial class Subject
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public string? Code { get; set; }
}
