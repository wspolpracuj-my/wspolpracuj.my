using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace wspolpracujmy.Models
{
    [Table("ProjectTags")]
    /// <summary>
    /// Powiązanie między projektem a tagiem (kategoria projektu).
    /// </summary>
    public class ProjectTag
    {
        [Required]
        [Column("project_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProjectId { get; set; }

        [Required]
        [Column("tag_id")]
        public int TagId { get; set; }

        [JsonIgnore]
        public required Project Project { get; set; }

        [JsonIgnore]
        public required Tag Tag { get; set; }
    }
}
