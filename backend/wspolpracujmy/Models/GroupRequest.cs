using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Newtonsoft.Json;

namespace wspolpracujmy.Models
{
    [Table("GroupRequests")]
    /// <summary>
    /// Żądanie dołączenia do grupy wysyłane przez studenta.
    /// </summary>
    public class GroupRequest
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("group_id")]
        public int GroupId { get; set; }

        [Column("student_id")]
        public int StudentId { get; set; }

        [Column("created_by_user_id")]
        public int CreatedByUserId { get; set; }

        [Column("status")]
        public GroupStatus Status { get; set; } = GroupStatus.Pending;

        [Column("type")]
        public string? Type { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("responded_at")]
        public DateTime? RespondedAt { get; set; } = null;
    }
}
