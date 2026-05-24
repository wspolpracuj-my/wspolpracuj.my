using System.ComponentModel.DataAnnotations;

namespace wspolpracujmy.DTOs.Auth
{
    /// <summary>
    /// Dane wejściowe wymagane do logowania użytkownika.
    /// </summary>
    public class LoginRequest
    {
        [Required]
        public required string Login { get; set; }

        [Required]
        public required string Password { get; set; }
    }
}