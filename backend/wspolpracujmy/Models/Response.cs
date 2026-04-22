using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace wspolpracujmy.Models
{
    [Table("Responses")]
    public class Response
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("comment_id")]
        public int CommentId { get; set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [Column("content")]
        public required string Content { get; set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        public required Comment Comment { get; set; }
        public required User User { get; set; }
    }
}
