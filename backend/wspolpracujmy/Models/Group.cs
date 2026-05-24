using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Newtonsoft.Json;

namespace wspolpracujmy.Models
{
    [Table("Groups")]
    /// <summary>
    /// Reprezentuje grupę studentów pracujących razem nad projektem.
    /// </summary>
    public class Group
    {
        public static readonly string[] AllowedStudentEmailDomains =
        {
            "g.elearn.uz.zgora.pl",
            "stud.uz.zgora.pl"
        };

        public static bool IsAllowedStudentEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            var atIndex = email.LastIndexOf('@');
            if (atIndex < 0 || atIndex == email.Length - 1)
                return false;

            var domain = email[(atIndex + 1)..].Trim().ToLowerInvariant();
            return AllowedStudentEmailDomains.Any(d => domain == d);
        }

        public static string NormalizeStudentEmail(string email)
            => email.Trim().ToLowerInvariant();

        public static bool StudentBelongsToTeam(Student? student)
            => student?.GroupId != null;
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("name")]
        public required string Name { get; set; }

        [Column("project_id")]
        public int? ProjectId { get; set; }

        [Column("is_accepted")]
        public GroupStatus? IsAccepted { get; set; }

        [Column("leader_id")]
        public int? LeaderId { get; set; }

        [Column("max_members")]
        public int? MaxMembers { get; set; }

        // `NumberOfMembers` is removed; compute members count from `Members` relationship instead.

        [JsonIgnore]
        public Project? Project { get; set; }

        [JsonIgnore]
        public Student? Leader { get; set; }

        [JsonIgnore]
        public ICollection<GroupRequest> GroupRequests { get; set; } = new List<GroupRequest>();

        [JsonIgnore]
        public ICollection<Student> Members { get; set; } = new List<Student>();

        [JsonIgnore]
        public ICollection<GroupFile> GroupFiles { get; set; } = new List<GroupFile>();

        [JsonIgnore]
        public ICollection<CalendarEvent> CalendarEvents { get; set; } = new List<CalendarEvent>();
    }
}
