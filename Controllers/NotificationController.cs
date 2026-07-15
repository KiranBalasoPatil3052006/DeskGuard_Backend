using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DeskGuardBackend.DTOs.Common;
using DeskGuardBackend.Entities;
using DeskGuardBackend.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace DeskGuardBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/v1/notifications")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(INotificationService notificationService, ILogger<NotificationController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        private long GetUserId()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(userIdStr, out var userId) ? userId : 0;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] bool unread_only = false)
        {
            try
            {
                var userId = GetUserId();
                var notifications = await _notificationService.GetUserNotificationsAsync(userId, unread_only);
                return Ok(ApiResponse<IEnumerable<Notification>>.Ok(notifications, "Notifications retrieved successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get notifications");
                return StatusCode(500, ApiResponse.Fail("Failed to retrieve notifications."));
            }
        }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkRead(long id)
        {
            try
            {
                await _notificationService.MarkAsReadAsync(id);
                return Ok(ApiResponse.Ok("Notification marked as read."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark notification read: {NotificationId}", id);
                return StatusCode(500, ApiResponse.Fail("Failed to mark notification as read."));
            }
        }

        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllRead()
        {
            try
            {
                var userId = GetUserId();
                await _notificationService.MarkAllAsReadAsync(userId);
                return Ok(ApiResponse.Ok("All notifications marked as read."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark all notifications read");
                return StatusCode(500, ApiResponse.Fail("Failed to mark notifications as read."));
            }
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> UnreadCount()
        {
            try
            {
                var userId = GetUserId();
                var notifications = await _notificationService.GetUserNotificationsAsync(userId, true);
                var count = notifications.Count();
                return Ok(ApiResponse<object>.Ok(new { unread_count = count }, "Unread count retrieved successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get unread count");
                return StatusCode(500, ApiResponse.Fail("Failed to retrieve unread count."));
            }
        }
    }
}
