using System;
using System.Threading.Tasks;
using Infrastructure.Security.Entities;
using Infrastructure.Security.Token;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _tokenService;
    private readonly Infrastructure.Persistence.Postgres.BonzerSchoolDbContext _db;

    public AuthController(UserManager<ApplicationUser> userManager, IJwtTokenService tokenService, Infrastructure.Persistence.Postgres.BonzerSchoolDbContext db)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _db = db;
    }

    /// <summary>JSON: { "email", "password" } (camelCase). Prefer class binding over positional records for broad serializer compatibility.</summary>
    public sealed class LoginRequest
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
    }

    public record RefreshRequest(string RefreshToken);

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest? req)
    {
        var email = req?.Email?.Trim();
        var password = req?.Password;
        if (string.IsNullOrEmpty(email) || password is null)
            return BadRequest("Email and password are required.");

        // Identity matches by normalized email; trailing spaces in UI often break lookup.
        var user = await _userManager.FindByEmailAsync(email)
            ?? await _userManager.FindByNameAsync(email);

        if (user == null)
            return Unauthorized("Invalid email or password.");

        if (await _userManager.IsLockedOutAsync(user))
            return Unauthorized("Account is locked after too many failed sign-ins. Try again later or reset your password.");

        if (!await _userManager.CheckPasswordAsync(user, password))
            return Unauthorized("Invalid email or password.");

        try
        {
            var (accessToken, refreshToken) = await _tokenService.GenerateTokensAsync(user);
            // Anonymous object guarantees System.Text.Json camelCase for the SPA.
            return Ok(new { accessToken, refreshToken });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "Login succeeded but the server could not issue tokens. Ensure PostgreSQL is running and refresh-token tables exist.",
                detail = ex.Message,
            });
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest req)
    {
        var tokenEntry = await _db.RefreshTokens.Include(t => t.User).SingleOrDefaultAsync(t => t.Token == req.RefreshToken);
        if (tokenEntry == null || !tokenEntry.IsActive) return BadRequest("Invalid or expired refresh token");

        // Revoke the incoming refresh token
        tokenEntry.Revoked = DateTime.UtcNow;
        _db.RefreshTokens.Update(tokenEntry);
        await _db.SaveChangesAsync();

        // Generate new tokens for the user
        var user = tokenEntry.User!;
        var (newAccess, newRefresh) = await _tokenService.GenerateTokensAsync(user);
        return Ok(new { accessToken = newAccess, refreshToken = newRefresh });
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] RefreshRequest req)
    {
        var revoked = await _tokenService.RevokeRefreshTokenAsync(req.RefreshToken);
        if (!revoked) return BadRequest("Token not found or already revoked");
        return NoContent();
    }
}