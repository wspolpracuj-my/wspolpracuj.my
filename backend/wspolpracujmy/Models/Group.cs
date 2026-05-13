using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace wspolpracujmy.Models
{
    [Table("Groups")]
    /// <summary>
    /// Reprezentuje grupę studentów pracujących razem nad projektem.
    /// </summary>
    public class Group
    {
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
