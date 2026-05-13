using System;

namespace wspolpracujmy.DTOs
{
    /// <summary>
    /// DTO powiadomienia rozszerzone dla administratorów (z informacją o użytkowniku).
    /// </summary>
    public class AdminNotificationDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string Content { get; set; } = string.Empty;
        public Models.NotificationStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? LinkTarget { get; set; }
        public int? GroupRequestId { get; set; }
    }
}
