using System;

namespace Domain.Entities
{
    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // ENROLLMENT, EARNINGS, REFUND, etc.
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? RelatedEntityId { get; set; } // For storing courseId, transactionId, etc.
        public string? AdditionalData { get; set; } // JSON string for any additional data

        // Navigation property if you want to relate to User entity
        public virtual User? User { get; set; }
    }
}
