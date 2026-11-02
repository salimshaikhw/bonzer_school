using Application.Common.Helpers.FireAndForget;
using Application.ViewModels.Account;
using BonzerSchool.UnitTests.TestSupport;
using Infrastructure.Security.Entities;
using Infrastructure.Security.Token;
using Infrastructure.Services.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Text;

namespace BonzerSchool.UnitTests.Auth;

public class UserServiceTests : IDisposable
{
    private readonly TestEnvironment _env = new();
    private readonly UserManager<ApplicationUser> _users = Substitute.For<UserManager<ApplicationUser>>(
        Substitute.For<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
    private readonly IFireAndForgetService _background = Substitute.For<IFireAndForgetService>();
    private readonly IJwtTokenService _tokens = Substitute.For<IJwtTokenService>();
    private readonly UserService _service;

    public UserServiceTests()
    {
        var roles = Substitute.For<RoleManager<IdentityRole>>(Substitute.For<IRoleStore<IdentityRole>>(), null, null, null, null);
        _service = new UserService(_users, roles, Options.Create(new JWTConfiguration()), _env.Catalog, TestEnvironment.Logger<UserService>(),
            new ConfigurationBuilder().Build(), _background, Substitute.For<IHttpContextAccessor>(), _env.Mapper, _env.TenantContext, _tokens);
    }

    [Fact]
    public async Task A_reset_link_request_for_an_unknown_email_looks_the_same_and_sends_nothing()
    {
        var answer = await _service.SendPasswordResetLinkAsync("nobody@school.edu");

        Assert.True(answer); // previously a 404, which told an attacker which emails have accounts
        _background.DidNotReceiveWithAnyArgs().FireAsync<Application.Abstractions.Messaging.IEmailService>(default!);
    }

    [Fact]
    public async Task A_reset_link_is_sent_for_a_known_email()
    {
        var user = new ApplicationUser { Id = "u1", Email = "asha@school.edu" };
        _users.FindByEmailAsync("asha@school.edu").Returns(user);
        _users.GeneratePasswordResetTokenAsync(user).Returns("token");

        Assert.True(await _service.SendPasswordResetLinkAsync(" asha@school.edu "));
        _background.ReceivedWithAnyArgs(1).FireAsync<Application.Abstractions.Messaging.IEmailService>(default!);
    }

    [Fact]
    public async Task Resetting_for_an_unknown_email_fails_like_an_invalid_link()
    {
        var result = await _service.ResetPasswordAsync(new ResetPasswordInput { Email = "nobody@school.edu", Code = "abc", Password = "NewPass@123" });

        Assert.False(result);
    }

    [Fact]
    public async Task A_successful_reset_ends_sessions_and_any_lockout()
    {
        var user = new ApplicationUser { Id = "u1", Email = "asha@school.edu" };
        _users.FindByEmailAsync("asha@school.edu").Returns(user);
        _users.ResetPasswordAsync(user, "token", "NewPass@123").Returns(IdentityResult.Success);
        var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("token"));

        Assert.True(await _service.ResetPasswordAsync(new ResetPasswordInput { Email = "asha@school.edu", Code = code, Password = "NewPass@123" }));

        await _tokens.Received(1).RevokeAllRefreshTokensAsync("u1", "password-reset");
        await _users.Received(1).ResetAccessFailedCountAsync(user);
        await _users.Received(1).SetLockoutEndDateAsync(user, null);
    }

    [Fact]
    public async Task Verification_links_are_not_sent_for_confirmed_or_unknown_emails()
    {
        _users.FindByEmailAsync("done@school.edu").Returns(new ApplicationUser { Id = "u2", Email = "done@school.edu", EmailConfirmed = true });

        Assert.True(await _service.SendEmailVerificationLinkAsync("done@school.edu"));
        Assert.True(await _service.SendEmailVerificationLinkAsync("nobody@school.edu"));
        _background.DidNotReceiveWithAnyArgs().FireAsync<Application.Abstractions.Messaging.IEmailService>(default!);
    }

    public void Dispose() => _env.Dispose();
}
