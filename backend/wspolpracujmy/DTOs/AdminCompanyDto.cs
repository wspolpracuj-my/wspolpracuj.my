namespace wspolpracujmy.DTOs
{
    /// <summary>
    /// DTO podsumowania firmy z dodatkowymi danymi konta (do widoku administratora).
    /// </summary>
    public class AdminCompanyDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string Login { get; set; } = string.Empty;
        public string? ContactEmail { get; set; }
    }
}
