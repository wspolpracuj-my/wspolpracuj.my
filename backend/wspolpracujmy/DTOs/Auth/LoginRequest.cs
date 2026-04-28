using System.ComponentModel.DataAnnotations;

namespace wspolpracujmy.DTOs.Auth
{
    public class LoginRequest
    {
        [Required]
        public required string Login { get; set; }

        [Required]
        public required string Password { get; set; }
    }
}