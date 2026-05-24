using System.ComponentModel.DataAnnotations;

namespace wspolpracujmy.DTOs
{
    /// <summary>
    /// DTO używane przez administratora do utworzenia konta firmy razem z użytkownikiem.
    /// </summary>
    public class AdminCompanyCreateDto
    {
        [Required]
        [StringLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Login { get; set; } = string.Empty;

        [Required]
        [MinLength(4)]
        [StringLength(200)]
        public string Password { get; set; } = string.Empty;

        [StringLength(320)]
        [EmailAddress]
        public string? ContactEmail { get; set; }
    }
}
