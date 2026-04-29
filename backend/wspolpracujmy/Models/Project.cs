using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace wspolpracujmy.Models
{
    [Table("Project")]
    /// <summary>
    /// Reprezentuje projekt tworzony w aplikacji.
    /// </summary>
    public class Project
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("company_id")]
        public int CompanyId { get; set; }

        [Required]
        [Column("topic")]
        public required string Topic { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("project_goal")]
        public string? ProjectGoal { get; set; }

        [Column("work_scope")]
        public string? WorkScope { get; set; }

        [Column("needed_technologies")]
        public string? NeededTechnologies { get; set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("max_groups")]
        public int? MaxGroups { get; set; }

        [Required]
        [Column("max_number_group_members")]
        public int MaxNumberGroupMembers { get; set; }

        [Required]
        [Column("meeting_type_id")]
        public int MeetingTypeId { get; set; }

        [Column("partnership_type")]
        public string? PartnershipType { get; set; }

        [Required]
        [Column("language_doc")]
        public required LanguageDoc LanguageDoc { get; set; }

        [Column("notes")]
        public string? Notes { get; set; }

        [Required]
        [Column("priority")]
        public required Priority Priority { get; set; }

        [JsonIgnore]
        public required Company Company { get; set; }

        [JsonIgnore]
        public required MeetingType MeetingType { get; set; }

        [JsonIgnore]
        public ICollection<Group> Groups { get; set; } = new List<Group>();

        [JsonIgnore]
        public ICollection<ProjectTag> ProjectTags { get; set; } = new List<ProjectTag>();

        [JsonIgnore]
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}
