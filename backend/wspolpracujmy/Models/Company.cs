using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace wspolpracujmy.Models
{
    [Table("Companies")]
    /// <summary>
    /// Reprezentuje firmę korzystającą z aplikacji.
    /// </summary>
    public class Company
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [Column("company_name")]
        public required string CompanyName { get; set; }

        [Column("contact_email")]
        public string? ContactEmail { get; set; }

        [JsonIgnore]
        public required User User { get; set; }

        [JsonIgnore]
        public ICollection<Project> Projects { get; set; } = new List<Project>();
    }
}
