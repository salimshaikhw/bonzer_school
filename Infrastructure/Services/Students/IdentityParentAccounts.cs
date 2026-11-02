using Application.Abstractions.Services.Students;
using Application.Abstractions.Tenancy;
using Application.Common.Constants;
using Infrastructure.Security.Entities;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Services.Students
{
    /// <inheritdoc cref="IParentAccounts"/>
    public class IdentityParentAccounts : IParentAccounts
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITenantContext _tenantContext;

        public IdentityParentAccounts(UserManager<ApplicationUser> userManager, ITenantContext tenantContext)
        {
            _userManager = userManager;
            _tenantContext = tenantContext;
        }

        public async Task<bool> IsParentLoginAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email.Trim());
            return user != null
                   && user.TenantId == _tenantContext.TenantId
                   && await _userManager.IsInRoleAsync(user, Roles.Parent);
        }
    }
}
