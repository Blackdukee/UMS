using System;
using System.Threading.Tasks;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using UserManagementAPI.Models;

namespace UserManagementAPI.Controllers
{
    [ApiController]
    [Route("api/v1/ums/notifications")]
    [Route("api/notifications")] // For inter-service communication
    [AllowAnonymous] // Allow access without authentication
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(INotificationService notificationService, ILogger<NotificationsController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> ProcessNotification([FromBody] NotificationRequest request)
        {
            _logger.LogInformation("Processing notification request: {Request}", request.AdditionalData);
            try
            {
                int parsedUserId = int.TryParse(request.UserId, out int userId) ? userId : 0;


                _logger.LogInformation("Received notification: {Action} for user {UserId}", request.Action, request.UserId);
                switch (request.Action)
                {
                    // parsing userid to int 


                    case "ENROLL_USER":
                        if (string.IsNullOrEmpty(request.CourseId) || string.IsNullOrEmpty(request.TransactionId))
                        {
                            return BadRequest(new { Success = false, Message = "CourseId and TransactionId are required." });
                        }
                        await _notificationService.ProcessEnrollUserNotificationAsync(parsedUserId, request.CourseId, request.TransactionId);
                        break;

                    case "NEW_EARNINGS":
                        if (string.IsNullOrEmpty(request.CourseId) || string.IsNullOrEmpty(request.TransactionId) || !request.Amount.HasValue)
                        {
                            return BadRequest(new { Success = false, Message = "CourseId, TransactionId, and Amount are required." });
                        }
                        await _notificationService.ProcessNewEarningsNotificationAsync(parsedUserId, request.CourseId, request.TransactionId, request.Amount.Value, request.TotalPendingEarnings ?? 0);
                        break;

                    case "EARNINGS_REFUNDED":
                        if (string.IsNullOrEmpty(request.CourseId) || string.IsNullOrEmpty(request.TransactionId) || !request.Amount.HasValue || string.IsNullOrEmpty(request.Reason))
                        {
                            return BadRequest(new { Success = false, Message = "CourseId, TransactionId, Amount, and Reason are required." });
                        }
                        await _notificationService.ProcessEarningsRefundedNotificationAsync(parsedUserId, request.CourseId, request.TransactionId, request.Amount.Value, request.Reason);
                        break;

                    case "REMOVE_ENROLLMENT":
                        if (string.IsNullOrEmpty(request.CourseId) || string.IsNullOrEmpty(request.TransactionId))
                        {
                            return BadRequest(new { Success = false, Message = "CourseId and TransactionId are required." });
                        }
                        await _notificationService.ProcessRemoveEnrollmentNotificationAsync(parsedUserId, request.CourseId, request.TransactionId);
                        break;

                    default:
                        return BadRequest(new { Success = false, Message = $"Unknown action type: {request.Action}" });
                }

                return Ok(new NotificationResponse { Success = true, Message = "Notification processed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing notification");
                return StatusCode(500, new NotificationResponse
                {
                    Success = false,
                    Message = "An error occurred while processing the notification"
                });
            }
        }
    }
}
