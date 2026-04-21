using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace wspolpracujmy.Models
{
    [Table("Files")]
    public class FileEntity
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [Column("original_name")]
        public string OriginalName { get; set; }

        [Required]
        [Column("gcs_bucket")]
        public string GcsBucket { get; set; }

        [Required]
        [Column("gcs_object_name")]
        public string GcsObjectName { get; set; }

        [Required]
        [Column("content_type")]
        public string ContentType { get; set; }

        [Required]
        [Column("size_bytes")]
        public long SizeBytes { get; set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        public User User { get; set; }
    }
}
