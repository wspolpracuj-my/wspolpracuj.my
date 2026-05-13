using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace wspolpracujmy.Models
{
    [Table("Tags")]
    /// <summary>
    /// Reprezentuje tag/kategorię przypisaną do projektu.
    /// </summary>
    public class Tag
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("name")]
        public required string Name { get; set; }

        [JsonIgnore]
        public ICollection<ProjectTag> ProjectTags { get; set; } = new List<ProjectTag>();
    }
}
