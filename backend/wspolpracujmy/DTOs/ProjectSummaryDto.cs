namespace wspolpracujmy.DTOs
{
    /// <summary>
    /// Skrócone informacje o projekcie do wyświetlania na listach.
    /// </summary>
    public class ProjectSummaryDto
    {
        public int Id { get; set; }
        public required string Topic { get; set; }
        public required string CompanyName { get; set; }
        public int CurrentGroupsCount { get; set; }
        public int MaxGroups { get; set; }
    }
}