using System.ComponentModel.DataAnnotations;

namespace wspolpracujmy.DTOs
{
    /// <summary>
    /// DTO używane do tworzenia nowego komentarza.
    /// </summary>
    public class CreateCommentDto
    {
        [Required]
        public int ProjectId { get; set; }

        /// <summary>Ustawiane po stronie serwera z tokena JWT — nie trzeba wysyłać z klienta.</summary>
        public int? UserId { get; set; }

        [Required]
        [StringLength(4000)]
        public string Content { get; set; } = string.Empty;
    }
}