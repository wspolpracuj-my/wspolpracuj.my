using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace wspolpracujmy.Models
{
    [Table("Comments")]
    /// <summary>
    /// Reprezentuje komentarz dotyczący projektu; zawiera treść, autora i czas utworzenia.
    /// </summary>
    public class Comment
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
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

        [JsonIgnore]
        public required User User { get; set; }

        [JsonIgnore]
        public required Project Project { get; set; }

        [JsonIgnore]
        public ICollection<Response> Responses { get; set; } = new List<Response>();
    }
}
