using System;

namespace wspolpracujmy.DTOs
{
    /// <summary>
    /// Powiadomienie do wyświetlenia w UI (tylko tekst, bez linków).
    /// </summary>
    public class NotificationDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
