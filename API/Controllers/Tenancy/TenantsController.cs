using Application.Abstractions.Tenancy;
using Application.Common.Constants;
using Application.ViewModels.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Tenancy
{
    /// <summary>
    /// Platform administration of tenants (school groups). Super Admin only.
    /// To work inside a tenant, the Super Admin sends the X-Tenant header with the tenant identifier.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = RoleGroups.PlatformAdmin)]
    public class TenantsController : ControllerBase
    {
        private readonly ITenantService _tenantService;

        public TenantsController(ITenantService tenantService)
        {
            _tenantService = tenantService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _tenantService.GetAllAsync());
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            return Ok(await _tenantService.GetByIdAsync(id));
        }

        /// <summary>Creates the tenant, its database and its first Group Admin login.</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTenantRequest request)
        {
            var tenant = await _tenantService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = tenant.Id }, tenant);
        }

        /// <summary>Deactivated tenants cannot sign in or call the API; their data is kept.</summary>
        [HttpPut("{id:guid}/status")]
        public async Task<IActionResult> SetStatus(Guid id, [FromBody] SetTenantStatusRequest request)
        {
            return Ok(await _tenantService.SetActiveAsync(id, request.IsActive));
        }

        /// <summary>Google Workspace domain / Microsoft Entra directory whose accounts may sign in with SSO.</summary>
        [HttpPut("{id:guid}/external-login")]
        public async Task<IActionResult> SetExternalLogin(Guid id, [FromBody] SetExternalLoginRequest request)
        {
            return Ok(await _tenantService.SetExternalLoginAsync(id, request));
        }

        /// <summary>Applies pending migrations to every active tenant database.</summary>
        [HttpPost("migrate")]
        public async Task<IActionResult> MigrateAll(CancellationToken cancellationToken)
        {
            return Ok(await _tenantService.MigrateAllAsync(cancellationToken));
        }
    }
}
