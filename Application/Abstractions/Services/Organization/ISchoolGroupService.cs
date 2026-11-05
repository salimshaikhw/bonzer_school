using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.ViewModels.SchoolGroup;

namespace Application.Abstractions.Services.Organization
{
    /// <summary>
    /// School groups = tenants. Lives in the catalog database (table "organizations").
    /// Super Admin sees and manages all; a tenant user only sees and edits their own group.
    /// </summary>
    public interface ISchoolGroupService
    {
        Task<IEnumerable<SchoolGroupDto>> GetAllAsync();
        Task<SchoolGroupDto> GetByIdAsync(Guid id);

        /// <summary>
        /// Registers the school group, creates and migrates its database, seeds reference data and
        /// creates its first Group Admin login. All-or-nothing. Super Admin only.
        /// </summary>
        Task<SchoolGroupDto> CreateAsync(CreateSchoolGroupInput input);

        Task<SchoolGroupDto> UpdateAsync(Guid id, UpdateSchoolGroupInput input);

        /// <summary>Inactive school groups cannot sign in or call the API; their data is kept.</summary>
        Task<SchoolGroupDto> SetActiveAsync(Guid id, bool isActive);

        Task<SchoolGroupDto> SetExternalLoginAsync(Guid id, SetExternalLoginRequest request);

        /// <summary>Applies pending migrations to every active school group database. Returns the identifiers migrated.</summary>
        Task<IReadOnlyList<string>> MigrateAllAsync(CancellationToken cancellationToken = default);
    }
}
