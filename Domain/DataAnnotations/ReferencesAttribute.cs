namespace Domain.DataAnnotations;

/// <summary>
/// Marks a foreign key column without a navigation property, e.g.
/// <c>[References(typeof(Student))] public Guid? StudentId { get; set; }</c>.
/// Deleting the referenced row is restricted unless <see cref="CascadeDelete"/> is set.
/// Applied by Infrastructure's ModelBuilder.ApplyDomainAnnotations().
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ReferencesAttribute(Type principal) : Attribute
{
    public Type Principal { get; } = principal;

    /// <summary>Delete this row when the referenced row is deleted.</summary>
    public bool CascadeDelete { get; init; }
}
