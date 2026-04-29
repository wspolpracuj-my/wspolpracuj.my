using System.ComponentModel.DataAnnotations;

namespace wspolpracujmy.Models
{
    /// <summary>
    /// DTO używane do tworzenia nowego komentarza.
    /// </summary>
    public class CreateCommentDto
    {
        [Required]
        public int ProjectId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(4000)]
        public string Content { get; set; } = string.Empty;
    }
}
