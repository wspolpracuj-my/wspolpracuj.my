using System;
using wspolpracujmy.Models;

namespace wspolpracujmy.DTOs
{
    /// <summary>
    /// DTO reprezentujące pojedyncze powiadomienie użytkownika.
    /// </summary>
    public class NotificationDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public NotificationStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? LinkTarget { get; set; }
    }
}