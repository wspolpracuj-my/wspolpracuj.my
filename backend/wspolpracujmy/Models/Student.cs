using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace wspolpracujmy.Models
{
    [Table("Students")]
    public class Student
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [Column("group_id")]
        public int GroupId { get; set; }

        [Required]
        [Column("email")]
        public required string Email { get; set; }

        public required User User { get; set; }
        public required Group Group { get; set; }
    }
}
