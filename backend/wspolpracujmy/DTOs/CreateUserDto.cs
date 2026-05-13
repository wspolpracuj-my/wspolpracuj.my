using System.ComponentModel.DataAnnotations;
using wspolpracujmy.Models;

namespace wspolpracujmy.DTOs
{
    /// <summary>
    /// DTO używane przy tworzeniu użytkownika.
    /// </summary>
    public class CreateUserDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Surname { get; set; } = string.Empty;

        [Required]
        public Role Role { get; set; }

        [Required]
        [StringLength(100)]
        public string Login { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Password { get; set; } = string.Empty;
    }
}