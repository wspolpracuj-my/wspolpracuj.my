using wspolpracujmy.Models;

namespace wspolpracujmy.DTOs.Auth
{
    /// <summary>
    /// Odpowiedź zwracana po poprawnej autoryzacji użytkownika.
    /// </summary>
    public class AuthResponse
    {
        public required string Token { get; set; }
        public int UserId { get; set; }
        public required string Login { get; set; }
        public required string FullName { get; set; }
        public Role Role { get; set; }
    }
}