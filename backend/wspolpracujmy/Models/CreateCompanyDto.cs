using System.ComponentModel.DataAnnotations;

namespace wspolpracujmy.Models
{
    /// <summary>
    /// DTO używane przy tworzeniu i aktualizacji firmy.
    /// </summary>
    public class CreateCompanyDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(320)]
        public string? ContactEmail { get; set; }
    }
}