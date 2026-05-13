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

        [JsonIgnore]
        public required Group Group { get; set; }

        [Column("project_id")]
        public int? ProjectId { get; set; }

        [JsonIgnore]
        public Project? Project { get; set; }

        [Column("student_id")]
        public int? StudentId { get; set; }

        [JsonIgnore]
        public Student? Student { get; set; }

        [Column("created_by_user_id")]
        public int CreatedByUserId { get; set; }

        [JsonIgnore]
        public User? CreatedByUser { get; set; }

        [Column("status")]
        public GroupStatus Status { get; set; } = GroupStatus.Pending;

        [Column("type")]
        public string? Type { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("responded_by_user_id")]
        public int? RespondedByUserId { get; set; }

        [Column("responded_at")]
        public DateTime? RespondedAt { get; set; } = null;

        [JsonIgnore]
        public User? RespondedByUser { get; set; }
    }
}
