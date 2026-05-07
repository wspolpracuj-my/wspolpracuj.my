namespace wspolpracujmy.DTOs
{
    /// <summary>
    /// Skrócone informacje o projekcie do wyświetlania na listach.
    /// </summary>
    public class ProjectSummaryDto
    {
        public int Id { get; set; }
        public string Topic { get; set; } = string.Empty;
        public int CurrentGroupsCount { get; set; }
        public int MaxGroups { get; set; }
    }
}