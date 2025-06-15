using Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface INotificationRepository
    {
        Task<Notification> AddAsync(Notification notification);
        Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId, bool includeRead = false, int limit = 50);
        Task<Notification?> GetByIdAsync(int notificationId);
        Task<bool> MarkAsReadAsync(int notificationId);
        Task<bool> MarkAllAsReadAsync(int userId);
        Task<bool> DeleteAsync(int notificationId);
        Task<int> GetUnreadCountAsync(int userId);
    }
}
