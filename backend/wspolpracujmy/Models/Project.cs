using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace wspolpracujmy.Models
{
    [Table("Project")]
    public class Project
    {
        [Key]
        [Column("id")]
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

        public required Company Company { get; set; }
        public required MeetingType MeetingType { get; set; }
        public ICollection<Group> Groups { get; set; } = new List<Group>();
        public ICollection<ProjectTag> ProjectTags { get; set; } = new List<ProjectTag>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}
