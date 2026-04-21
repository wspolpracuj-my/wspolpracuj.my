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
        public string Name { get; set; }

        [Required]
        [Column("project_id")]
        public int ProjectId { get; set; }

        [Required]
        [Column("is_accepted")]
        public GroupStatus IsAccepted { get; set; }

        [Required]
        [Column("leader_id")]
        public int LeaderId { get; set; }

        [Required]
        [Column("number_of_members")]
        public int NumberOfMembers { get; set; }

        public Project Project { get; set; }
        public Student Leader { get; set; }
        public ICollection<Student> Members { get; set; }
        public ICollection<GroupFile> GroupFiles { get; set; }
        public ICollection<CalendarEvent> CalendarEvents { get; set; }
    }
}
