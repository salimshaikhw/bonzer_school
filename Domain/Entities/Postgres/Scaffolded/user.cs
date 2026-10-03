using System;
using System.Collections.Generic;

namespace Domain.Entities.Postgres.Scaffolded;

public partial class User
{

    public Guid Id { get; set; }

    public Guid? BranchId { get; set; }

    public Guid? RoleId { get; set; }

    public Guid? StaffId { get; set; }

    public string? Username { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? PasswordHash { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? LastLogin { get; set; }

    public string? SecurityQuestion { get; set; }

    public string? SecurityAnswer { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? CreatedAt { get; set; }
}
