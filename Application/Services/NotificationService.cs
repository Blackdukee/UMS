using System;
using System.Text.Json;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;
        private readonly IUserRepository _userRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IEmailService _emailService;

        public NotificationService(
            ILogger<NotificationService> logger,
            IUserRepository userRepository,
            INotificationRepository notificationRepository,
            IEmailService emailService)
        {
            _logger = logger;
            _userRepository = userRepository;
            _notificationRepository = notificationRepository;
            _emailService = emailService;
        }

        public async Task ProcessEnrollUserNotificationAsync(int userId, string courseId, string transactionId)
        {
            try
            {
                // Get user from repository
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Cannot process enrollment notification: User {UserId} not found", userId);
                    return;
                }

                // Create notification message
                string title = "Course Enrollment Successful";
                string message = $"You have been successfully enrolled in course {courseId}. Transaction ID: {transactionId}";
                
                // Send notification
                await SendNotificationAsync(userId, title, message, "ENROLLMENT");
                
                _logger.LogInformation("Processed enrollment notification for user {UserId}, course {CourseId}", userId, courseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing enrollment notification for user {UserId}", userId);
                throw;
            }
        }        public async Task ProcessNewEarningsNotificationAsync(int userId, string courseId, string transactionId, decimal amount, decimal totalPendingEarnings)
        {
            try
            {
                // Get user from repository
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Cannot process earnings notification: User {UserId} not found", userId);
                    return;
                }

                // Log the user details to check if they're properly identified as educators
                _logger.LogInformation("Processing earnings notification for user {UserId}, Role: {Role}", userId, user.Role);

                // Create notification message
                string title = "New Earnings";
                string message = $"You have received ${amount:F2} from course {courseId}. Your pending earnings balance is now ${totalPendingEarnings:F2}";
                
                // Send notification
                await SendNotificationAsync(userId, title, message, "EARNINGS");
                
                _logger.LogInformation("Processed earnings notification for user {UserId}, amount {Amount}", userId, amount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing earnings notification for user {UserId}", userId);
                throw;
            }
        }        public async Task ProcessEarningsRefundedNotificationAsync(int userId, string courseId, string transactionId, decimal amount, string reason)
        {
            try
            {
                // Get user from repository
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Cannot process refund notification: User {UserId} not found", userId);
                    return;
                }

                // Log the user details to check if they're properly identified as educators
                _logger.LogInformation("Processing refund notification for user {UserId}, Role: {Role}", userId, user.Role);

                // Create notification message
                string title = "Earnings Refunded";
                string message = $"${amount:F2} has been refunded from your earnings for course {courseId}. Reason: {reason}";
                  // Create additional data for the refund
                var additionalData = new
                {
                    courseId,
                    transactionId,
                    amount,
                    reason,
                    refundDate = DateTime.UtcNow
                };
                
                // Send notification with additional data
                await SendNotificationAsync(userId, title, message, "REFUND", JsonSerializer.Serialize(additionalData));
                
                _logger.LogInformation("Processed refund notification for user {UserId}, amount {Amount}, courseId: {CourseId}", userId, amount, courseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing refund notification for user {UserId}", userId);
                throw;
            }
        }        public async Task ProcessRemoveEnrollmentNotificationAsync(int userId, string courseId, string transactionId)
        {
            try
            {
                // Get user from repository
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Cannot process enrollment removal notification: User {UserId} not found", userId);
                    return;
                }

                // Log the user details to check if they're properly identified
                _logger.LogInformation("Processing enrollment removal notification for user {UserId}, Role: {Role}", userId, user.Role);

                // Create notification message
                string title = "Course Enrollment Removed";
                string message = $"Your enrollment in course {courseId} has been removed. Transaction ID: {transactionId}";
                
                // Send notification
                await SendNotificationAsync(userId, title, message, "ENROLLMENT_REMOVAL");
                
                _logger.LogInformation("Processed enrollment removal notification for user {UserId}, course {CourseId}, transactionId: {TransactionId}", userId, courseId, transactionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing enrollment removal notification for user {UserId}", userId);
                throw;
            }
        }        public async Task<bool> SendNotificationAsync(int userId, string title, string message, string type, string? additionalData = null)
        {
            try
            {
                // Get user to check role
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Attempted to send notification to non-existent user {UserId}", userId);
                    return false;
                }// Debug log to see exactly what role the user has
                _logger.LogInformation("Processing notification for user {UserId} with role {Role}", userId, user.Role);

                // For educators, save notification to database - use fixed string comparison and trim
                string userRole = user.Role?.Trim() ?? "";
                if (string.Equals(userRole, "Educator", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("User {UserId} identified as Educator, saving to database", userId);                    var notification = new Notification
                    {
                        UserId = userId,
                        Title = title,
                        Message = message,
                        Type = type,
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false,
                        RelatedEntityId = type == "REFUND" || type == "EARNINGS" || type == "ENROLLMENT" || type == "ENROLLMENT_REMOVAL" ? 
                            message.Contains("course") ? 
                                message.Split("course")[1].Split('.')[0].Trim() : null : null,
                        AdditionalData = additionalData
                    };
                    
                    await _notificationRepository.AddAsync(notification);
                    _logger.LogInformation("DB Notification saved for educator {UserId}: {Title}", userId, title);
                }
                // For students and other roles, send email
                else
                {
                    _logger.LogInformation("User {UserId} identified as {Role}, sending email", userId, user.Role ?? "unknown role");
                    await _emailService.SendEmailAsync(user.Email, title, message);
                    _logger.LogInformation("Email notification sent to user {UserId}: {Title}", userId, title);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification to user {UserId}", userId);
                return false;
            }
        }
    }
}
