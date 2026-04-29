using System.Collections.Generic;

namespace wspolpracujmy.Models
{
    /// <summary>
    /// DTO zawierające szczegółowe informacje o projekcie.
    /// </summary>
    public class ProjectDetailsDto
    {
        public int Id { get; set; }
        public string Topic { get; set; } = string.Empty;
        public string? ProjectGoal { get; set; }
        public string? WorkScope { get; set; }
        public string? NeededTechnologies { get; set; }
        public int? MaxGroups { get; set; }
        public int MaxNumberGroupMembers { get; set; }
        public LanguageDoc LanguageDoc { get; set; }
        public Priority Priority { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
    }
}
