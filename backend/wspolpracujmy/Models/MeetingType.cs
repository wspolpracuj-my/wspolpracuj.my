using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace wspolpracujmy.Models
{
    [Table("Meeting_types")]
    /// <summary>
    /// Typ spotkania/terminu używany w kalendarzu.
    /// </summary>
    public class MeetingType
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        [Required]
        [Column("type")]
        public required string Type { get; set; }
    }
}
