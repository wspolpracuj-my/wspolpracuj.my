using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace wspolpracujmy.Models
{
    [Table("ProjectTags")]
    public class ProjectTag
    {
        [Required]
        [Column("project_id")]
        public int ProjectId { get; set; }

        [Required]
        [Column("tag_id")]
        public int TagId { get; set; }

        public required Project Project { get; set; }
        public required Tag Tag { get; set; }
    }
}
