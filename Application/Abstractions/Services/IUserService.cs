using Application.ViewModels.Account;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface IUserService
    {
        Task<RegistrationDetails> RegisterAsync(RegisterInput model);
        Task<IEnumerable<RefreshToken>> GetUserRefreshTokensAsync(string id);
        Task<bool> ConfirmEmailAsyc(string userId, string code);
        Task<bool> SendPasswordResetLinkAsync(string email);
        Task<bool> ResetPasswordAsync(ResetPasswordInput input);
        Task<bool> SendEmailVerificationLinkAsync(string email);
        Task<bool> ChangePasswordAsync(ChangePasswordInput input);
        Task<bool> AddRoleAsync(RoleInput input);
        Task<bool> RemoveRoleAsync(RoleInput input);
        Task<IEnumerable<ApplicationUserDetails>> GetAllUsersAsync();
        Task<ApplicationUserDetails> GetUserAsync(string id);
        Task<bool> UpdateUserAsync(string id, ApplicationUserInput input);
    }
}


