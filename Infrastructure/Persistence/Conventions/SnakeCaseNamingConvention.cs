using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace Infrastructure.Persistence.Conventions;

/// <summary>
/// Database naming for the school entities (namespace Domain.Entities and its feature sub-namespaces):
///   column       StudentId          -> student_id
///   primary key  {table}_pkey       (id uuid DEFAULT gen_random_uuid())
///   foreign key  {table}_{column}_fkey
///   index        ix_{table}_{columns}
/// Table names are always set explicitly with [Table]. Anything configured explicitly wins over
/// this convention. ASP.NET Identity tables are not touched.
/// </summary>
public sealed partial class SnakeCaseNamingConvention : IModelFinalizingConvention
{
    private const string EntityNamespace = "Domain.Entities";

    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
        {
            if (!IsSchoolEntity(entityType.ClrType))
                continue;

            var table = entityType.GetTableName()!;

            foreach (var property in entityType.GetProperties())
                property.Builder.HasColumnName(ToSnakeCase(property.Name));

            var primaryKey = entityType.FindPrimaryKey();
            if (primaryKey != null)
            {
                primaryKey.Builder.HasName($"{table}_pkey");

                if (primaryKey.Properties is [var keyProperty] && keyProperty.ClrType == typeof(Guid))
                    keyProperty.Builder.HasDefaultValueSql("gen_random_uuid()");
            }

            foreach (var foreignKey in entityType.GetForeignKeys())
            {
                var columns = string.Join("_", foreignKey.Properties.Select(p => ToSnakeCase(p.Name)));
                foreignKey.Builder.HasConstraintName($"{table}_{columns}_fkey");
            }

            foreach (var index in entityType.GetIndexes())
            {
                var columns = string.Join("_", index.Properties.Select(p => ToSnakeCase(p.Name)));
                index.Builder.HasDatabaseName($"ix_{table}_{columns}");
            }
        }
    }

    public static bool IsSchoolEntity(Type clrType) =>
        clrType.Namespace is { } ns && (ns == EntityNamespace || ns.StartsWith(EntityNamespace + ".", StringComparison.Ordinal));

    public static string ToSnakeCase(string name)
    {
        var withBreaks = LowerToUpper().Replace(name, "$1_$2");
        withBreaks = AcronymToWord().Replace(withBreaks, "$1_$2");
        return withBreaks.ToLowerInvariant();
    }

    // "studentId" -> "student_Id", "line1Address" -> "line1_Address"
    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex LowerToUpper();

    // "HTMLParser" -> "HTML_Parser"
    [GeneratedRegex("([A-Z])([A-Z][a-z])")]
    private static partial Regex AcronymToWord();
}
