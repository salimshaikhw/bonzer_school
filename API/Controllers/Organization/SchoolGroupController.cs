using Application.Abstractions.Services.Organization;
using Application.Common.Constants;
using Application.ViewModels.SchoolGroup;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Organization
{
    /// <summary>
    /// School groups. A school group is the tenant: it owns one database and has many branches.
    /// Super Admin creates and manages all of them; a tenant user sees and edits only their own.
    /// To work inside a school group the Super Admin sends the X-Tenant header with its identifier.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SchoolGroupController : ControllerBase
    {
        private readonly ISchoolGroupService _service;

        public SchoolGroupController(ISchoolGroupService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id) => Ok(await _service.GetByIdAsync(id));

        /// <summary>Creates the school group, its database and its first Group Admin login.</summary>
        [HttpPost]
        [Authorize(Roles = RoleGroups.PlatformAdmin)]
        public async Task<IActionResult> Create([FromBody] CreateSchoolGroupInput input)
        {
            var result = await _service.CreateAsync(input);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = RoleGroups.GroupAdminOnly + "," + RoleGroups.PlatformAdmin)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSchoolGroupInput request) =>
            Ok(await _service.UpdateAsync(id, request));

        /// <summary>Deactivated school groups cannot sign in or call the API; their data is kept.</summary>
        [HttpPut("{id:guid}/status")]
        [Authorize(Roles = RoleGroups.PlatformAdmin)]
        public async Task<IActionResult> SetStatus(Guid id, [FromBody] SetSchoolGroupStatusRequest request) =>
            Ok(await _service.SetActiveAsync(id, request.IsActive));

        /// <summary>Google Workspace domain / Microsoft Entra directory whose accounts may sign in with SSO.</summary>
        [HttpPut("{id:guid}/external-login")]
        [Authorize(Roles = RoleGroups.PlatformAdmin)]
        public async Task<IActionResult> SetExternalLogin(Guid id, [FromBody] SetExternalLoginRequest request) =>
            Ok(await _service.SetExternalLoginAsync(id, request));

        /// <summary>Applies pending migrations to every active school group database.</summary>
        [HttpPost("migrate")]
        [Authorize(Roles = RoleGroups.PlatformAdmin)]
        public async Task<IActionResult> MigrateAll(CancellationToken cancellationToken) =>
            Ok(await _service.MigrateAllAsync(cancellationToken));
    }
}
