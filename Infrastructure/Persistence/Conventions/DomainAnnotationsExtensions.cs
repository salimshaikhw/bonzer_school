using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using Domain.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Conventions;

/// <summary>
/// Applies the Domain.DataAnnotations attributes, which describe what standard data annotations
/// cannot express: database defaults, foreign keys without navigation properties, soft-delete
/// query filters and filtered indexes. Standard annotations ([Table], [MaxLength], [Index]...)
/// are handled by EF Core itself.
/// </summary>
public static class DomainAnnotationsExtensions
{
    public static ModelBuilder ApplyDomainAnnotations(this ModelBuilder modelBuilder)
    {
        var entityTypes = modelBuilder.Model.GetEntityTypes()
            .Where(t => SnakeCaseNamingConvention.IsSchoolEntity(t.ClrType))
            .Select(t => t.ClrType)
            .ToList();

        foreach (var clrType in entityTypes)
        {
            var entity = modelBuilder.Entity(clrType);

            if (clrType.GetCustomAttribute<SoftDeleteAttribute>() != null)
                entity.HasQueryFilter(SoftDeleteFilter(clrType));

            foreach (var index in clrType.GetCustomAttributes<FilteredIndexAttribute>())
                entity.HasIndex(index.PropertyNames)
                      .IsUnique(index.IsUnique)
                      .HasFilter(index.Filter);

            foreach (var property in clrType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetCustomAttribute<DefaultValueAttribute>() is { } defaultValue)
                    entity.Property(property.Name).HasDefaultValue(defaultValue.Value);

                if (property.GetCustomAttribute<DefaultValueSqlAttribute>() is { } defaultSql)
                    entity.Property(property.Name).HasDefaultValueSql(defaultSql.Sql);

                if (property.GetCustomAttribute<ReferencesAttribute>() is { } reference)
                    entity.HasOne(reference.Principal)
                          .WithMany()
                          .HasForeignKey(property.Name)
                          .OnDelete(reference.CascadeDelete ? DeleteBehavior.Cascade : DeleteBehavior.Restrict);
            }
        }

        return modelBuilder;
    }

    // e => e.IsDeleted != true
    private static LambdaExpression SoftDeleteFilter(Type clrType)
    {
        var e = Expression.Parameter(clrType, "e");
        var isDeleted = Expression.Property(e, "IsDeleted");
        var notDeleted = Expression.NotEqual(isDeleted, Expression.Constant(true, isDeleted.Type));
        return Expression.Lambda(notDeleted, e);
    }
}
