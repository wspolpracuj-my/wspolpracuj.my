using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace wspolpracujmy.Models
{
    [Table("Notifications")]
    /// <summary>
    /// Reprezentuje powiadomienie wysyłane do użytkownika.
    /// </summary>
    public class Notification
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [Column("content")]
        public required string Content { get; set; }

        [Required]
        [Column("status")]
        public required NotificationStatus Status { get; set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("link_target")]
        public string? LinkTarget { get; set; }

        [JsonIgnore]
        public required User User { get; set; }
    }
}
