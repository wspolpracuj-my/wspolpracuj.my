using System.ComponentModel.DataAnnotations;

namespace wspolpracujmy.DTOs
{
    /// <summary>
    /// DTO używane do tworzenia odpowiedzi na komentarz.
    /// </summary>
    public class CreateResponseDto
    {
        [Required]
        public int CommentId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(2000)]
        public string Content { get; set; } = string.Empty;
    }
}