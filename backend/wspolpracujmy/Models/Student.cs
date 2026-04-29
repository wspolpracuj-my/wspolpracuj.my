using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace wspolpracujmy.Models
{
    [Table("Students")]
    /// <summary>
    /// Reprezentuje studenta korzystającego z aplikacji.
    /// </summary>
    public class Student
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        [Column("group_id")]
        public int GroupId { get; set; }

        [Required]
        [Column("email")]
        public required string Email { get; set; }

        [JsonIgnore]
        public required User User { get; set; }

        [JsonIgnore]
        public required Group Group { get; set; }
    }
}
