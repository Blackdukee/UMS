using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly AppDbContext _context;

        public NotificationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Notification> AddAsync(Notification notification)
        {
            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();
            return notification;
        }

        public async Task<bool> DeleteAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification == null)
                return false;

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Notification?> GetByIdAsync(int notificationId)
        {
            return await _context.Notifications.FindAsync(notificationId);
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .CountAsync();
        }

        public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId, bool includeRead = false, int limit = 50)
        {
            var query = _context.Notifications.AsQueryable();

            query = query.Where(n => n.UserId == userId);
            
            if (!includeRead)
                query = query.Where(n => !n.IsRead);

            query = query.OrderByDescending(n => n.CreatedAt)
                .Take(limit);

            return await query.ToListAsync();
        }

        public async Task<bool> MarkAllAsReadAsync(int userId)
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            if (!unreadNotifications.Any())
                return false;

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkAsReadAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification == null)
                return false;

            notification.IsRead = true;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
