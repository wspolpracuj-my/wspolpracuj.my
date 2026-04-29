namespace wspolpracujmy.Models
{
    /// <summary>
    /// Skrócone informacje o firmie do wyświetlania w listach.
    /// </summary>
    public class CompanySummaryDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? ContactEmail { get; set; }
    }
}
