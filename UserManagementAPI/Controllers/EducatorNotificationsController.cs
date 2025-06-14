using System;
using System.Threading.Tasks;
using Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace UserManagementAPI.Controllers
{
    [ApiController]
    [Route("api/v1/ums/notifications")]
    [Authorize] // Ensure users are authenticated
    public class EducatorNotificationsController : ControllerBase
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly ILogger<EducatorNotificationsController> _logger;

        public EducatorNotificationsController(
            INotificationRepository notificationRepository,
            ILogger<EducatorNotificationsController> logger)
        {
            _notificationRepository = notificationRepository;
            _logger = logger;
        }

        // Get user's notifications
        [HttpGet("user")]
        public async Task<IActionResult> GetUserNotifications([FromQuery] bool includeRead = false)
        {
            try
            {
                // Get user ID from claims
                if (!int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out int userId))
                {
                    return Unauthorized(new { error = "Invalid user identity" });
                }

                // Get user role from claims and validate it's an educator
                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                if (string.IsNullOrEmpty(userRole) || !userRole.Equals("Educator", StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid();
                }

                var notifications = await _notificationRepository.GetUserNotificationsAsync(userId, includeRead);
                var unreadCount = await _notificationRepository.GetUnreadCountAsync(userId);

                return Ok(new { notifications, unreadCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications for user");
                return StatusCode(500, new { error = "An error occurred while retrieving notifications" });
            }
        }

        // Mark notification as read
        [HttpPatch("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                // Get user ID from claims
                if (!int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out int userId))
                {
                    return Unauthorized(new { error = "Invalid user identity" });
                }

                // First, get the notification
                var notification = await _notificationRepository.GetByIdAsync(id);
                if (notification == null)
                {
                    return NotFound(new { error = "Notification not found" });
                }

                // Verify ownership
                if (notification.UserId != userId)
                {
                    return Forbid();
                }

                // Mark as read
                var success = await _notificationRepository.MarkAsReadAsync(id);
                if (!success)
                {
                    return StatusCode(500, new { error = "Failed to update notification" });
                }

                return Ok(new { message = "Notification marked as read" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read");
                return StatusCode(500, new { error = "An error occurred while updating the notification" });
            }
        }

        // Mark all notifications as read
        [HttpPost("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                // Get user ID from claims
                if (!int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out int userId))
                {
                    return Unauthorized(new { error = "Invalid user identity" });
                }

                await _notificationRepository.MarkAllAsReadAsync(userId);
                return Ok(new { message = "All notifications marked as read" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return StatusCode(500, new { error = "An error occurred while updating notifications" });
            }
        }
    }
}
