namespace Domain.DataAnnotations;

/// <summary>
/// Rows with <c>IsDeleted == true</c> are hidden from every query (global query filter).
/// Use IgnoreQueryFilters() to reach them. The entity must have a <c>bool? IsDeleted</c> property.
/// Applied by Infrastructure's ModelBuilder.ApplyDomainAnnotations().
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class SoftDeleteAttribute : Attribute
{
}
