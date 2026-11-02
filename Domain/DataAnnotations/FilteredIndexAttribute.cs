namespace Domain.DataAnnotations;

/// <summary>
/// Partial index, e.g. <c>[FilteredIndex(nameof(StaffCode), Filter = "staff_code IS NOT NULL", IsUnique = true)]</c>.
/// Use EF Core's [Index] for indexes without a filter. The filter is SQL, so it uses column names.
/// Applied by Infrastructure's ModelBuilder.ApplyDomainAnnotations().
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class FilteredIndexAttribute(params string[] propertyNames) : Attribute
{
    public string[] PropertyNames { get; } = propertyNames;

    public required string Filter { get; init; }

    public bool IsUnique { get; init; }
}
