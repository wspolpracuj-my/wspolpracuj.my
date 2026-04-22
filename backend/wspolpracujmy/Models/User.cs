using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace wspolpracujmy.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("name")]
        public required string Name { get; set; }

        [Required]
        [Column("surname")]
        public required string Surname { get; set; }

        [Required]
        [Column("role")]
        public required Role Role { get; set; }

        [Required]
        [Column("login")]
        public required string Login { get; set; }

        [Required]
        [Column("password")]
        public required string Password { get; set; }

        public Company? Company { get; set; }
        public Student? Student { get; set; }
        public ICollection<FileEntity> Files { get; set; } = new List<FileEntity>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<Response> Responses { get; set; } = new List<Response>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
