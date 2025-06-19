using System.Text.Json.Serialization;

namespace UserManagementAPI.Models
{
    public class NotificationRequest
    {
        public string UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? CourseId { get; set; }
        public string? TransactionId { get; set; }
        public decimal? Amount { get; set; }
        public decimal? TotalPendingEarnings { get; set; }
        public string? Reason { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object>? AdditionalData { get; set; }
    }

    public class NotificationResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }
}
