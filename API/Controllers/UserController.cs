using Application.Abstractions.Services;
using Application.ViewModels.Account;
using Infrastructure.Security.Token;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;
        private readonly JWTConfiguration _jwtConfiguration;

        public UserController(IUserService userService, ILogger<UserController> logger, IOptions<JWTConfiguration> jwtOptions)
        {
            _userService = userService;
            _logger = logger;
            _jwtConfiguration = jwtOptions?.Value ?? new JWTConfiguration();
        }

        // GET: api/user
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<ApplicationUserDetails>>> GetAll()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        // GET: api/user/{id}
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<ApplicationUserDetails>> Get(string id)
        {
            var user = await _userService.GetUserAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        // GET: api/user/{id}/refresh-tokens
        [HttpGet("{id}/refresh-tokens")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<Application.ViewModels.Account.RefreshToken>>> GetRefreshTokens(string id)
        {
            var tokens = await _userService.GetUserRefreshTokensAsync(id);
            return Ok(tokens);
        }

        // POST: api/user/register
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<RegistrationDetails>> Register([FromBody] RegisterInput model)
        {
            var result = await _userService.RegisterAsync(model);
            if (result.IsSuccessful)
                return CreatedAtAction(nameof(Get), new { id = result.Message }, result);

            return BadRequest(result);
        }

        // POST: api/user/confirm-email?userId=...&code=...
        [HttpPost("confirm-email")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string code)
        {
            var succeeded = await _userService.ConfirmEmailAsyc(userId, code);
            if (succeeded) return Ok();
            return BadRequest();
        }

        // POST: api/user/send-password-reset?email=...
        [HttpPost("send-password-reset")]
        [AllowAnonymous]
        public async Task<IActionResult> SendPasswordReset([FromQuery] string email)
        {
            var sent = await _userService.SendPasswordResetLinkAsync(email);
            if (sent) return Ok();
            return BadRequest();
        }

        // POST: api/user/reset-password
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordInput input)
        {
            var ok = await _userService.ResetPasswordAsync(input);
            if (ok) return Ok();
            return BadRequest();
        }

        // POST: api/user/send-email-verification?email=...
        [HttpPost("send-email-verification")]
        [Authorize]
        public async Task<IActionResult> SendEmailVerification([FromQuery] string email)
        {
            var sent = await _userService.SendEmailVerificationLinkAsync(email);
            if (sent) return Ok();
            return BadRequest();
        }

        // POST: api/user/change-password
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordInput input)
        {
            var ok = await _userService.ChangePasswordAsync(input);
            if (ok) return Ok();
            return BadRequest();
        }

        // POST: api/user/add-role
        [HttpPost("add-role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddRole([FromBody] RoleInput input)
        {
            var ok = await _userService.AddRoleAsync(input);
            if (ok) return Ok();
            return BadRequest();
        }

        // POST: api/user/remove-role
        [HttpPost("remove-role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveRole([FromBody] RoleInput input)
        {
            var ok = await _userService.RemoveRoleAsync(input);
            if (ok) return Ok();
            return BadRequest();
        }

        // PUT: api/user/{id}
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(string id, [FromBody] ApplicationUserInput input)
        {
            var ok = await _userService.UpdateUserAsync(id, input);
            if (ok) return NoContent();
            return BadRequest();
        }
    }
}