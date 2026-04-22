using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace wspolpracujmy.Models
{
    [Table("GroupRequests")]
    public class GroupRequest
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("group_id")]
        public int GroupId { get; set; }

        [Column("student_id")]
        public int StudentId { get; set; }

        [Column("created_by_user_id")]
        public int CreatedByUserId { get; set; }

        [Column("status")]
        public string? Status { get; set; }

        [Column("type")]
        public string? Type { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("responded_at")]
        public DateTime? RespondedAt { get; set; }
    }
}
