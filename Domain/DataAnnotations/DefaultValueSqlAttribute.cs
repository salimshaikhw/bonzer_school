namespace Domain.DataAnnotations;

/// <summary>
/// Database default given as SQL, e.g. <c>[DefaultValueSql("CURRENT_TIMESTAMP")]</c>.
/// For constant defaults use <see cref="System.ComponentModel.DefaultValueAttribute"/>.
/// Applied by Infrastructure's ModelBuilder.ApplyDomainAnnotations().
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DefaultValueSqlAttribute(string sql) : Attribute
{
    public string Sql { get; } = sql;
}
