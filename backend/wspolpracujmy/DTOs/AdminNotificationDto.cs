using System;

namespace wspolpracujmy.DTOs
{
    /// <summary>
    /// DTO pojedynczego powiadomienia w widoku administratora.
    /// Zawiera nadawcę (jeśli rozpoznany), odbiorcę i treść.
    /// </summary>
    public class AdminNotificationDto
    {
        public int Id { get; set; }
        public int ToUserId { get; set; }
        public string ToName { get; set; } = string.Empty;
        public int? FromUserId { get; set; }
        public string? FromName { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
