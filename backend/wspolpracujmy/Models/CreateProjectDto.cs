using System.ComponentModel.DataAnnotations;

namespace wspolpracujmy.Models
{
    /// <summary>
    /// DTO używane przy tworzeniu nowego projektu.
    /// </summary>
    public class CreateProjectDto
    {
        [Required]
        public int CompanyId { get; set; }

        [Required]
        [StringLength(200)]
        public string Topic { get; set; } = string.Empty;

        [StringLength(4000)]
        public string? Description { get; set; }

        public string? ProjectGoal { get; set; }
        public string? WorkScope { get; set; }
        public string? NeededTechnologies { get; set; }

        public int? MaxGroups { get; set; }

        [Required]
        public int MaxNumberGroupMembers { get; set; }

        [Required]
        public int MeetingTypeId { get; set; }

        public string? PartnershipType { get; set; }

        [Required]
        public LanguageDoc LanguageDoc { get; set; }

        public string? Notes { get; set; }

        [Required]
        public Priority Priority { get; set; }
    }
}
