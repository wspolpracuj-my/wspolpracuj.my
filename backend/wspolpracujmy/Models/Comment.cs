using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace wspolpracujmy.Models
{
    [Table("Comments")]
    public class Comment
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [Column("project_id")]
        public int ProjectId { get; set; }

        [Required]
        [Column("content")]
        public required string Content { get; set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        public required User User { get; set; }
        public required Project Project { get; set; }
        public ICollection<Response> Responses { get; set; } = new List<Response>();
    }
}
