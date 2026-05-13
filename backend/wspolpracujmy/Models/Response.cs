using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace wspolpracujmy.Models
{
    [Table("Responses")]
    /// <summary>
    /// Reprezentuje odpowiedź na komentarz w dyskusji.
    /// </summary>
    public class Response
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
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

        [JsonIgnore]
        public required Comment Comment { get; set; }

        [JsonIgnore]
        public required User User { get; set; }
    }
}
