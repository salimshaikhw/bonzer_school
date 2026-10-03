using System;
using System.Collections.Generic;

namespace Infrastructure;

public partial class ParentUser
{
    public Guid Id { get; set; }

    public Guid? StudentId { get; set; }

    public string? Username { get; set; }

    public string? PasswordHash { get; set; }

    public string? Phone { get; set; }
}
