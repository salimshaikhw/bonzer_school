namespace Domain.Common;

/// <summary>Primary key shared by every table: <c>id uuid DEFAULT gen_random_uuid()</c>.</summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; }
}
