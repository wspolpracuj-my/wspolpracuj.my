using System;
using System.Collections.Generic;
using wspolpracujmy.Models;

namespace wspolpracujmy.DTOs
{
    /// <summary>
    /// DTO zawierające szczegółowe informacje o projekcie.
    /// </summary>
    public class ProjectDetailsDto
    {
        public int Id { get; set; }
        public required string Topic { get; set; }
        public required string CompanyName { get; set; }
        public string? Description { get; set; }
        public string? ProjectGoal { get; set; }
        public string? WorkScope { get; set; }
        public string? NeededTechnologies { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? MaxGroups { get; set; }
        public int MaxNumberGroupMembers { get; set; }
        public int CurrentGroupsCount { get; set; }
        public int MeetingTypeId { get; set; }
        public string? MeetingTypeName { get; set; }
        public string? PartnershipType { get; set; }
        public LanguageDoc LanguageDoc { get; set; }
        public Priority Priority { get; set; }
        public string? Notes { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
    }
}
