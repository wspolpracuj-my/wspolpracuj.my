using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace wspolpracujmy.Models
{
    [Table("Users")]
    /// <summary>
    /// Reprezentuje użytkownika systemu (student, firma, admin).
    /// </summary>
    public class User
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("name")]
        public required string Name { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("surname")]
        public required string Surname { get; set; }

        [Required]
        [Column("role")]
        public Role Role { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("login")]
        public required string Login { get; set; }

        [Required]
        [Column("password_hash")]
        public required string PasswordHash { get; set; }

        [JsonIgnore]
        public Company? Company { get; set; }

        [JsonIgnore]
        public Student? Student { get; set; }

        [JsonIgnore]
        public ICollection<FileEntity> Files { get; set; } = new List<FileEntity>();

        [JsonIgnore]
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();

        [JsonIgnore]
        public ICollection<Response> Responses { get; set; } = new List<Response>();

        [JsonIgnore]
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}