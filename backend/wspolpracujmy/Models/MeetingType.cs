using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace wspolpracujmy.Models
{
    [Table("Meeting_types")]
    public class MeetingType
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("type")]
        public required string Type { get; set; }
    }
}
