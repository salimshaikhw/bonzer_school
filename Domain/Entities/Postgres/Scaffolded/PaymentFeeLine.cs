using System;

namespace Domain.Entities.Postgres.Scaffolded;

public partial class PaymentFeeLine
{
    public Guid Id { get; set; }

    public Guid PaymentId { get; set; }

    /// <summary>Null for ad-hoc lines recorded with <see cref="CustomFeeName"/> only.</summary>
    public Guid? FeeHeadId { get; set; }

    public decimal Amount { get; set; }

    public string? CustomFeeName { get; set; }
}
