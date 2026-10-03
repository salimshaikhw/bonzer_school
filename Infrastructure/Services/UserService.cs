using Application.Abstractions.Messaging;
using Application.Abstractions.Services;
using Application.Common;
using Application.Common.Constants;
using Application.Common.Extensions;
using Application.Common.Helpers.FireAndForget;
using Application.Exceptions;
using Application.ViewModels.Account;
using AutoMapper;
using Infrastructure.Persistence.Postgres;
using Infrastructure.Security.Entities;
using Infrastructure.Security.Token;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using RefreshToken = Infrastructure.Security.Entities.RefreshToken;

namespace Infrastructure.Services
{
    public class UserService : ServiceException, IUserService
    {
        private readonly BonzerSchoolDbContext _applicationDbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly JWTConfiguration _jwtConfiguration;
        private readonly ILogger<UserService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IFireAndForgetService _fireAndForgetService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMapper _mapper;

        public UserService(UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IOptions<JWTConfiguration> jwtConfiguration,
            BonzerSchoolDbContext context,
            ILogger<UserService> logger,
            IConfiguration configuration,
            IFireAndForgetService fireAndForgetService,
            IHttpContextAccessor httpContextAccessor, IMapper mapper)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _jwtConfiguration = jwtConfiguration.Value;
            _applicationDbContext = context;
            _logger = logger;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _fireAndForgetService = fireAndForgetService;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ApplicationUserDetails>> GetAllUsersAsync()
        {
            try
            {
                var query = _applicationDbContext.Users.AsNoTracking().AsQueryable()
                     .SelectMany(
                         user => _applicationDbContext.UserRoles.Where(userRoleMapEntry => user.Id == userRoleMapEntry.UserId).DefaultIfEmpty(),
                         (user, roleMapEntry) => new { User = user, RoleMapEntry = roleMapEntry })
                     .SelectMany(
                         x => _applicationDbContext.Roles.Where(role => role.Id == x.RoleMapEntry.RoleId).DefaultIfEmpty(),
                         (x, role) => new { x.User, Role = role });

                query = query.Where(x => x.Role.Name != Roles.Admin);
                query = query.OrderByDescending(x => x.User.LastName);
                var displayResult = await query.ToListAsync();

                var data = displayResult.Aggregate(
                     new Dictionary<ApplicationUser, List<IdentityRole>>(), // seed
                     (dict, data) =>
                     {
                         dict.TryAdd(data.User, new List<IdentityRole>());
                         if (null != data.Role)
                         {
                             dict[data.User].Add(data.Role);
                         }
                         return dict;
                     }, x => x).Select(x => new ApplicationUserDetails
                     {
                         AccessFailedCount = x.Key.AccessFailedCount,
                         Email = x.Key.Email,
                         EmailConfirmed = x.Key.EmailConfirmed,
                         FullName = $"{x.Key.FirstName} {x.Key.LastName}",
                         Id = x.Key.Id,
                         LockoutEnabled = x.Key.LockoutEnabled,
                         LockoutEnd = x.Key.LockoutEnd?.ToString("dd-MM-yyyy hh:mm tt"),
                         PhoneNumber = x.Key.PhoneNumber,
                         PhoneNumberConfirmed = x.Key.PhoneNumberConfirmed,
                         TwoFactorEnabled = x.Key.TwoFactorEnabled,

                     }).ToList();

                return data;
            }
            catch (Exception ex)
            {
                throw HandleException(ex);
            }
        }

        public async Task<RegistrationDetails> RegisterAsync(RegisterInput model)
        {
            using (var transaction = await _applicationDbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    model.Validate();

                    var user = new ApplicationUser
                    {
                        UserName = model.Email,
                        Email = model.Email,
                        FirstName = model.FirstName,
                        LastName = model.LastName
                    };
                    var userWithSameEmail = await _userManager.FindByEmailAsync(model.Email);
                    if (userWithSameEmail == null)
                    {
                        var result = await _userManager.CreateAsync(user, model.Password);
                        if (result.Succeeded)
                        {
                            _logger.LogInformation($"User {model.Email} created a new account with password.");

                            // Assign the requested role if it exists
                            var requestedRole = model.Role?.Trim();
                            var roleExists = await _roleManager.RoleExistsAsync(requestedRole);
                            if (!string.IsNullOrEmpty(requestedRole) && roleExists)
                            {
                                result = await _userManager.AddToRoleAsync(user, requestedRole);
                                if (result.Succeeded)
                                {
                                    _logger.LogInformation($"User added to {requestedRole} role.");
                                }
                                else
                                {
                                    await transaction.RollbackAsync();
                                    return new RegistrationDetails { IsSuccessful = false, Message = string.Join(',', result.Errors.Select(x => x.Description)) };
                                }
                            }
                            else
                            {
                                await transaction.RollbackAsync();
                                return new RegistrationDetails { IsSuccessful = false, Message = $"Role '{requestedRole}' does not exist." };
                            }

                            var baseUrl = _configuration.GetSection("appSettings")["BaseUrl"];
                            var code = _userManager.GenerateEmailConfirmationTokenAsync(user).Result;
                            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                            var callbackUrl = $"{baseUrl}/account/confirm-email?code={code}&userId={user.Id}";

                            _fireAndForgetService.FireAsync<Application.Abstractions.Messaging.IEmailService>(async (sender) =>
                            {
                                // Send him an confirmation email here:
                                await sender.SendEmailAsync(new List<string> { model.Email }, " Confirm your email",
                                    $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");
                            });

                            await transaction.CommitAsync();
                            return new RegistrationDetails { IsSuccessful = true, Message = $"User Registered with username {user.UserName}" };
                        }
                        else
                        {
                            await transaction.RollbackAsync();
                            return new RegistrationDetails { IsSuccessful = false, Message = string.Join(',', result.Errors.Select(x => x.Description)) };
                        }
                    }
                    else
                    {
                        await transaction.RollbackAsync();
                        return new RegistrationDetails { IsSuccessful = false, Message = $"Email {user.Email} is already registered." };
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw HandleException(ex);
                }
            }
        }

        public async Task<bool> ConfirmEmailAsyc(string userId, string code)
        {
            try
            {
                if (userId == null || code == null)
                {
                    return false;
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    throw new BaseServiceException($"Unable to load user with ID '{userId}'.", ExceptionCodes.ItemNotFound);
                }

                code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
                var result = await _userManager.ConfirmEmailAsync(user, code);
                return result.Succeeded;
            }
            catch (Exception ex)
            {
                throw HandleException(ex);
            }
        }

        public async Task<AuthenticationDetails> GetTokenAsync(TokenRequestInput model)
        {
            var authenticationModel = new AuthenticationDetails();
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                authenticationModel.IsAuthenticated = false;
                authenticationModel.Message = $"No accounts registered with {model.Email}.";
                return authenticationModel;
            }
            if (await _userManager.CheckPasswordAsync(user, model.Password))
            {
                authenticationModel.IsAuthenticated = true;
                authenticationModel.Message = "Successful";
                JwtSecurityToken jwtSecurityToken = await CreateJwtToken(user);
                authenticationModel.Token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
                authenticationModel.Email = user.Email;
                authenticationModel.UserName = user.UserName;
                var rolesList = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
                authenticationModel.Roles = rolesList.ToList();


                if (user.RefreshTokens.Any(a => a.IsActive))
                {
                    var activeRefreshToken = user.RefreshTokens.Where(a => a.IsActive == true).FirstOrDefault();
                    authenticationModel.RefreshToken = activeRefreshToken.Token;
                    authenticationModel.RefreshTokenExpiration = activeRefreshToken.Expires;
                }
                else
                {
                    var refreshToken = CreateRefreshToken();
                    authenticationModel.RefreshToken = refreshToken.Token;
                    authenticationModel.RefreshTokenExpiration = refreshToken.Expires;
                    user.RefreshTokens.Add(refreshToken);
                    _applicationDbContext.Update(user);
                    _applicationDbContext.SaveChanges();
                }

                return authenticationModel;
            }
            authenticationModel.IsAuthenticated = false;
            authenticationModel.Message = $"Incorrect credentials for user {user.Email}.";
            return authenticationModel;
        }

        private async Task<JwtSecurityToken> CreateJwtToken(ApplicationUser user)
        {
            var userClaims = await _userManager.GetClaimsAsync(user);
            var roles = await _userManager.GetRolesAsync(user);

            var roleClaims = new List<Claim>();

            for (int i = 0; i < roles.Count; i++)
            {
                roleClaims.Add(new Claim("roles", roles[i]));
            }

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("uid", user.Id)
            }
            .Union(userClaims)
            .Union(roleClaims);

            var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfiguration.Key));
            var signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

            var jwtSecurityToken = new JwtSecurityToken(
                issuer: _jwtConfiguration.Issuer,
                audience: _jwtConfiguration.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtConfiguration.TokenExpiryDurationInMinutes),
                signingCredentials: signingCredentials);
            return jwtSecurityToken;
        }

        public async Task<AuthenticationDetails> RefreshTokenAsync(string token)
        {
            var authenticationModel = new AuthenticationDetails();
            var user = _applicationDbContext.Users.SingleOrDefault(u => u.RefreshTokens.Any(t => t.Token == token));
            if (user == null)
            {
                authenticationModel.IsAuthenticated = false;
                authenticationModel.Message = $"Token did not match any users.";
                return authenticationModel;
            }

            var refreshToken = user.RefreshTokens.Single(x => x.Token == token);

            if (!refreshToken.IsActive)
            {
                authenticationModel.IsAuthenticated = false;
                authenticationModel.Message = $"Token Not Active.";
                return authenticationModel;
            }

            //Revoke Current Refresh Token
            refreshToken.Revoked = DateTime.UtcNow;

            //Generate new Refresh Token and save to Database
            var newRefreshToken = CreateRefreshToken();
            user.RefreshTokens.Add(newRefreshToken);
            _applicationDbContext.Update(user);
            _applicationDbContext.SaveChanges();

            //Generates new jwt
            authenticationModel.IsAuthenticated = true;
            JwtSecurityToken jwtSecurityToken = await CreateJwtToken(user);
            authenticationModel.Token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
            authenticationModel.Email = user.Email;
            authenticationModel.UserName = user.UserName;
            var rolesList = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
            authenticationModel.Roles = rolesList.ToList();
            authenticationModel.RefreshToken = newRefreshToken.Token;
            authenticationModel.RefreshTokenExpiration = newRefreshToken.Expires;
            return authenticationModel;
        }

        public async Task<bool> SendPasswordResetLinkAsync(string email)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(email))
                {
                    var user = _userManager.FindByEmailAsync(email).Result;
                    if (user != null)
                    {
                        var baseUrl = _configuration.GetSection("appSettings")["BaseUrl"];

                        var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                        var callbackUrl = $"{baseUrl}/account/reset-password?code={code}";

                        _fireAndForgetService.FireAsync<Application.Abstractions.Messaging.IEmailService>(async (sender) =>
                        {
                            await sender.SendEmailAsync(new List<string> { email }, "Reset Password", $"Please reset your password by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");
                        });

                        return true;
                    }
                    else
                    {
                        throw new BaseServiceException($"User not {email} found.", ExceptionCodes.ItemNotFound);
                    }
                }
                else
                {
                    throw new BaseServiceException("Invalid username/email.", ExceptionCodes.Validation);
                }
            }
            catch (Exception ex)
            {
                throw HandleException(ex);
            }

        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordInput input)
        {
            try
            {
                input.Validate();

                var user = await _userManager.FindByEmailAsync(input.Email);
                if (user == null)
                {
                    throw new BaseServiceException("User not found", ExceptionCodes.ItemNotFound);
                }

                var code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(input.Code));

                var result = await _userManager.ResetPasswordAsync(user, code, input.Password);
                return result.Succeeded;
            }
            catch (Exception ex)
            {
                throw HandleException(ex);
            }
        }

        public async Task<bool> SendEmailVerificationLinkAsync(string email)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(email))
                {
                    var user = _userManager.FindByEmailAsync(email).Result;
                    if (user != null)
                    {
                        var baseUrl = _configuration.GetSection("appSettings")["BaseUrl"];
                        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                        var callbackUrl = $"{baseUrl}/account/confirm-email?code={code}&userId={user.Id}";

                        _fireAndForgetService.FireAsync<Application.Abstractions.Messaging.IEmailService>(async (sender) =>
                        {
                            await sender.SendEmailAsync(new List<string> { email }, " Confirm your email", $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");
                        });

                        return true;
                    }
                    else
                    {
                        throw new BaseServiceException($"User not {email} found.", ExceptionCodes.ItemNotFound);
                    }
                }
                else
                {
                    throw new BaseServiceException("Invalid username/email.", ExceptionCodes.Validation);
                }
            }
            catch (Exception ex)
            {
                throw HandleException(ex);
            }
        }

        public async Task<bool> ChangePasswordAsync(ChangePasswordInput input)
        {
            try
            {
                input.Validate();

                var user = await _userManager.FindByEmailAsync(_httpContextAccessor.HttpContext?.User.GetUserEmail());
                if (user == null)
                {
                    throw new BaseServiceException($"Unable to load user with ID '{_userManager.GetUserId(_httpContextAccessor.HttpContext.User)}'.", ExceptionCodes.ItemNotFound);
                }

                var changePasswordResult = await _userManager.ChangePasswordAsync(user, input.OldPassword, input.NewPassword);
                if (!changePasswordResult.Succeeded)
                {
                    return false;
                }
                else
                {
                    await _userManager.UpdateAsync(user);
                    return true;
                }
            }
            catch (Exception ex)
            {
                throw HandleException(ex);
            }
        }

        public async Task<bool> AddRoleAsync(RoleInput input)
        {
            try
            {
                input.Validate();
                var user = await _userManager.FindByEmailAsync(input.Email);
                if (user == null)
                {
                    throw new BaseServiceException($"No Accounts Registered with {input.Email}.", ExceptionCodes.ItemNotFound);
                }

                var roleExists = typeof(Roles).GetAllPublicConstantValues<string>().Any(x => x.ToLower() == input.Role.ToLower());
                if (roleExists)
                {
                    var validRole = typeof(Roles).GetAllPublicConstantValues<string>().Where(x => x.ToString().ToLower() == input.Role.ToLower()).FirstOrDefault();
                    await _userManager.AddToRoleAsync(user, validRole);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                throw HandleException(ex);
            }
        }

        public async Task<bool> UpdateUserAsync(string id, ApplicationUserInput input)
        {
            try
            {
                input.Validate();
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    throw new BaseServiceException($"No accounts registered with {id}.", ExceptionCodes.ItemNotFound);
                }

                user.FirstName = input.FirstName;
                user.LastName = input.LastName;
                var identityResult = await _userManager.UpdateAsync(user);

                return identityResult.Succeeded;
            }
            catch (Exception ex)
            {
                throw HandleException(ex);
            }
        }

        public async Task<bool> RemoveRoleAsync(RoleInput input)
        {
            try
            {
                input.Validate();
                var user = await _userManager.FindByEmailAsync(input.Email);
                if (user == null)
                {
                    throw new BaseServiceException($"No Accounts Registered with {input.Email}.", ExceptionCodes.ItemNotFound);
                }

                var roleExists = typeof(Roles).GetAllPublicConstantValues<string>().Any(x => x.ToLower() == input.Role.ToLower());
                if (roleExists)
                {
                    var validRole = typeof(Roles).GetAllPublicConstantValues<string>().Where(x => x.ToString().ToLower() == input.Role.ToLower()).FirstOrDefault();
                    var result = await _userManager.RemoveFromRolesAsync(user, new List<string> { validRole });
                    if (result.Succeeded)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                throw HandleException(ex);
            }
        }

        public async Task<bool> RevokeToken(string token)
        {
            var user = _applicationDbContext.Users.SingleOrDefault(u => u.RefreshTokens.Any(t => t.Token == token));

            if (user == null)
            {
                return false;
            }

            var refreshToken = user.RefreshTokens.Single(x => x.Token == token);

            if (!refreshToken.IsActive)
            {
                return false;
            }

            refreshToken.Revoked = DateTime.UtcNow;
            _applicationDbContext.Update(user);
            await _applicationDbContext.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<RefreshToken>> GetUserRefreshTokensAsync(string id)
        {
            try
            {
                var user = await GetById(id);
                return user.RefreshTokens;
            }
            catch (Exception ex)
            {
                throw HandleException(ex);
            }
        }

        public async Task<ApplicationUserDetails> GetUserAsync(string id)
        {
            try
            {
                var user = await GetById(id);
                return _mapper.Map<ApplicationUserDetails>(user);
            }
            catch (Exception ex)
            {
                throw HandleException(ex);
            }
        }

        #region Private Methods

        private RefreshToken CreateRefreshToken()
        {
            var randomNumber = new byte[32];
            System.Security.Cryptography.RandomNumberGenerator.Fill(randomNumber);
            return new RefreshToken
            {
                Token = Convert.ToBase64String(randomNumber),
                Expires = DateTime.UtcNow.AddDays(_jwtConfiguration.RefreshTokenExpiryDurationInDays),
                Created = DateTime.UtcNow
            };
        }

        private async Task<ApplicationUser> GetById(string id)
        {
            try
            {
                var user = await _applicationDbContext.Users.FindAsync(id);
                if (user == null)
                {
                    throw new BaseServiceException($"User with id {id} not found.", ExceptionCodes.ItemNotFound);
                }

                return user;
            }
            catch (Exception ex)
            {
                throw HandleException(ex);
            }
        }

        Task<IEnumerable<Application.ViewModels.Account.RefreshToken>> IUserService.GetUserRefreshTokensAsync(string id)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
