using Application.Abstractions.Services.Academics;
using Application.Abstractions.Services.Personnel;
using Application.ViewModels.StaffRole;
using AutoMapper;
using Infrastructure.Services.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Personnel
{
    /// <summary>School roles held by a staff member. Rules: staff and role exist; a role is held only once.</summary>
    public class StaffRoleService : CrudService<StaffRole, StaffRoleRequest, StaffRoleResponse>, IStaffRoleService
    {
        private readonly IAcademicStructure _structure;
        private readonly IRoleRepository _roles;

        public StaffRoleService(ILogger<StaffRoleService> logger, IStaffRoleRepository repository, IMapper mapper,
            IAcademicStructure structure, IRoleRepository roles)
            : base(repository, mapper, logger)
        {
            _structure = structure;
            _roles = roles;
        }

        protected override string EntityName => "StaffRole";

        protected override Task ValidateCreateAsync(StaffRoleRequest request) => ValidateAsync(request, exceptId: null);

        protected override Task ValidateUpdateAsync(StaffRole existing, StaffRoleRequest request) => ValidateAsync(request, existing.Id);

        private async Task ValidateAsync(StaffRoleRequest request, Guid? exceptId)
        {
            await _structure.EnsureStaffExistsAsync(request.StaffId);
            if (request.RoleId is { } roleId && !await _roles.IsExistAsync(r => r.Id == roleId))
                throw RuleViolation($"Role {roleId} does not exist.");

            if (await Repository.IsExistAsync(r => r.StaffId == request.StaffId && r.RoleId == request.RoleId && r.Id != exceptId))
                throw Conflict("The staff member already has this role.");
        }
    }
}
