using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace wspolpracujmy.Models
{
    [Table("Groups")]
    public class Group
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("name")]
        public required string Name { get; set; }

        [Required]
        [Column("project_id")]
        public int ProjectId { get; set; }

        [Required]
        [Column("is_accepted")]
        public required GroupStatus IsAccepted { get; set; }

        [Column("leader_id")]
        public int? LeaderId { get; set; }

        // `NumberOfMembers` is removed; compute members count from `Members` relationship instead.

        public required Project Project { get; set; }
        public Student? Leader { get; set; }
        public ICollection<Student> Members { get; set; } = new List<Student>();
        public ICollection<GroupFile> GroupFiles { get; set; } = new List<GroupFile>();
        public ICollection<CalendarEvent> CalendarEvents { get; set; } = new List<CalendarEvent>();
    }
}
