namespace wspolpracujmy.Models
{
    /// <summary>
    /// Skrócone informacje o użytkowniku do zwracania po utworzeniu.
    /// </summary>
    public class UserSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public Role Role { get; set; }
        public string Login { get; set; } = string.Empty;
    }
}