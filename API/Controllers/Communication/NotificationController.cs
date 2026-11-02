using Application.Common.Constants;
using Application.Abstractions.Services;
using Application.ViewModels.Notification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace API.Controllers.Communication
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationController> _logger;
        public NotificationController(INotificationService notificationService, ILogger<NotificationController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _notificationService.GetAllAsync();
            return Ok(result);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _notificationService.GetByIdAsync(id);
            return Ok(result);
        }
        [HttpPost]
        [Authorize(Roles = RoleGroups.AllStaff)]
        public async Task<IActionResult> Create([FromBody] NotificationRequest request)
        {
            var result = await _notificationService.CreateAsync(request);
            return Ok(result);
        }
        [HttpPut("{id}")]
        [Authorize(Roles = RoleGroups.AllStaff)]
        public async Task<IActionResult> Update(Guid id, [FromBody] NotificationRequest request)
        {
            var result = await _notificationService.UpdateAsync(id, request);
            return Ok(result);
        }
        [HttpDelete("{id}")]
        [Authorize(Roles = RoleGroups.AllStaff)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _notificationService.DeleteAsync(id);
            return Ok(result);
        }
    }
}