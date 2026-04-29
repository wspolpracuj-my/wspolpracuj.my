using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace wspolpracujmy.Models
{
    [Table("Files")]
    /// <summary>
    /// Reprezentuje plik przechowywany w systemie (metadane pliku).
    /// </summary>
    public class FileEntity
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        [Column("group_id")]
        public int? GroupId { get; set; }

        [Required]
        [Column("original_name")]
        public required string OriginalName { get; set; }

        [Required]
        [Column("gcs_bucket")]
        public required string GcsBucket { get; set; }

        [Required]
        [Column("gcs_object_name")]
        public required string GcsObjectName { get; set; }

        [Required]
        [Column("content_type")]
        public required string ContentType { get; set; }

        [Required]
        [Column("size_bytes")]
        public long SizeBytes { get; set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonIgnore]
        public required User User { get; set; }
    }
}
