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
        public string Name { get; set; }

        [Required]
        [Column("surname")]
        public string Surname { get; set; }

        [Required]
        [Column("role")]
        public Role Role { get; set; }

        [Required]
        [Column("login")]
        public string Login { get; set; }

        [Required]
        [Column("password")]
        public string Password { get; set; }

        public Company Company { get; set; }
        public Student Student { get; set; }
        public ICollection<FileEntity> Files { get; set; }
        public ICollection<Comment> Comments { get; set; }
        public ICollection<Response> Responses { get; set; }
        public ICollection<Notification> Notifications { get; set; }
    }
}
