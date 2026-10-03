using System;

namespace Domain.Entities.Postgres.Scaffolded;

public partial class SchoolHoliday
{
    public Guid Id { get; set; }

    public Guid BranchId { get; set; }

    public DateOnly HolidayDate { get; set; }

    public string? Name { get; set; }

    public DateTime? CreatedAt { get; set; }

    public bool? IsDeleted { get; set; }
}
