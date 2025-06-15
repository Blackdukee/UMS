using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface INotificationService
    {
        Task ProcessEnrollUserNotificationAsync(int userId, string courseId, string transactionId);
        Task ProcessNewEarningsNotificationAsync(int userId, string courseId, string transactionId, decimal amount, decimal totalPendingEarnings);
        Task ProcessEarningsRefundedNotificationAsync(int userId, string courseId, string transactionId, decimal amount, string reason);
        Task ProcessRemoveEnrollmentNotificationAsync(int userId, string courseId, string transactionId);
        Task<bool> SendNotificationAsync(int userId, string title, string message, string type, string? additionalData = null);
    }
}
