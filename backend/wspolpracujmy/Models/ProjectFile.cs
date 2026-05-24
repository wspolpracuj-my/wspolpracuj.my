using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace wspolpracujmy.Models
{
    [Table("ProjectFiles")]
    /// <summary>
    /// Reprezentuje metadane pliku projektu przechowywanego w GCS.
    /// </summary>
    public class ProjectFile
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("original_file_name")]
        public required string OriginalFileName { get; set; }

        [Required]
        [MaxLength(512)]
        [Column("gcs_object_name")]
        public required string GcsObjectName { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("content_type")]
        public required string ContentType { get; set; }

        [Required]
        [Column("team_id")]
        public int TeamId { get; set; }

        [Required]
        [Column("upload_date")]
        public DateTime UploadDate { get; set; }
    }
}