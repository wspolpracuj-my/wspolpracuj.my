using System.ComponentModel.DataAnnotations;

namespace wspolpracujmy.DTOs
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

        // For admin-only creation: require password confirmation
        [Required]
        [StringLength(200, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [StringLength(200, MinimumLength = 6)]
        public string PasswordConfirm { get; set; } = string.Empty;
    }
}