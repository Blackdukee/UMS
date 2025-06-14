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
            try
            {
                _logger.LogInformation("Received notification: {Action} for user {UserId}", request.Action, request.UserId); switch (request.Action)
                {
                    case "ENROLL_USER":
                        if (string.IsNullOrEmpty(request.CourseId) || string.IsNullOrEmpty(request.TransactionId))
                        {
                            return BadRequest(new NotificationResponse
                            {
                                Success = false,
                                Message = "CourseId and TransactionId are required for ENROLL_USER action"
                            });
                        }

                        await _notificationService.ProcessEnrollUserNotificationAsync(
                            request.UserId,
                            request.CourseId,
                            request.TransactionId);
                        break;

                    case "NEW_EARNINGS":
                        if (request.Data == null || string.IsNullOrEmpty(request.Data.CourseId) ||
                            string.IsNullOrEmpty(request.Data.TransactionId) || !request.Data.Amount.HasValue)
                        {
                            return BadRequest(new NotificationResponse
                            {
                                Success = false,
                                Message = "Missing required data for NEW_EARNINGS action"
                            });
                        }

                        await _notificationService.ProcessNewEarningsNotificationAsync(
                            request.UserId,
                            request.Data.CourseId,
                            request.Data.TransactionId,
                            request.Data.Amount.Value,
                            request.Data.TotalPendingEarnings ?? 0);
                        break;
                    case "EARNINGS_REFUNDED":
                        if (request.Data == null || string.IsNullOrEmpty(request.Data.CourseId) ||
                            string.IsNullOrEmpty(request.Data.TransactionId) || !request.Data.Amount.HasValue ||
                            string.IsNullOrEmpty(request.Data.Reason))
                        {
                            return BadRequest(new NotificationResponse
                            {
                                Success = false,
                                Message = "Missing required data for EARNINGS_REFUNDED action"
                            });
                        }

                        await _notificationService.ProcessEarningsRefundedNotificationAsync(
                            request.UserId,
                            request.Data.CourseId,
                            request.Data.TransactionId,
                            request.Data.Amount.Value,
                            request.Data.Reason);
                        break;

                    case "REMOVE_ENROLLMENT":
                        if (string.IsNullOrEmpty(request.CourseId) || string.IsNullOrEmpty(request.TransactionId))
                        {
                            return BadRequest(new NotificationResponse
                            {
                                Success = false,
                                Message = "CourseId and TransactionId are required for REMOVE_ENROLLMENT action"
                            });
                        }

                        await _notificationService.ProcessRemoveEnrollmentNotificationAsync(
                            request.UserId,
                            request.CourseId,
                            request.TransactionId);
                        break;

                    default:
                        return BadRequest(new NotificationResponse
                        {
                            Success = false,
                            Message = $"Unknown action type: {request.Action}"
                        });
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
