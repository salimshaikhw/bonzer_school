using System.Data.Common;
using Application.Common;
using Application.Exceptions;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Infrastructure.Tenancy;

/// <summary>
/// Added to BonzerSchoolDbContext when the scope has no tenant. The context can still be constructed
/// (so services that only sometimes touch school data work), but the first query fails with a clear
/// message instead of reaching any database.
/// </summary>
public sealed class TenantRequiredInterceptor : DbConnectionInterceptor
{
    public static readonly TenantRequiredInterceptor Instance = new();

    public override InterceptionResult<DbConnection> ConnectionCreating(
        ConnectionCreatingEventData eventData, InterceptionResult<DbConnection> result) =>
        throw new BaseServiceException(
            "No school group (tenant) is selected for this request. Sign in with a school account, " +
            "or as Super Admin send the X-Tenant header.", ExceptionCodes.Validation);
}
