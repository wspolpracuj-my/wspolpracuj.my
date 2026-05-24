using System.ComponentModel.DataAnnotations;
using wspolpracujmy.Models;

namespace wspolpracujmy.DTOs.Auth
{
    /// <summary>
    /// Dane wejściowe używane podczas rejestracji nowego użytkownika.
    /// </summary>
    public class RegisterRequest
    {
        [Required]
        [MaxLength(100)]
        public required string Name { get; set; }

        [Required]
        [MaxLength(100)]
        public required string Surname { get; set; }

        [Required]
        [MaxLength(100)]
        public required string Login { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public required string Email { get; set; }

        [Required]
        [MinLength(6)]
        public required string Password { get; set; }

        [Required]
        public Role Role { get; set; }
    }
}