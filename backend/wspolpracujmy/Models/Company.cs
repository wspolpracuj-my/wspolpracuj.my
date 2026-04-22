using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace wspolpracujmy.Models
{
    [Table("Companies")]
    public class Company
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [Column("company_name")]
        public required string CompanyName { get; set; }

        [Column("contact_email")]
        public string? ContactEmail { get; set; }

        public required User User { get; set; }
        public ICollection<Project> Projects { get; set; } = new List<Project>();
    }
}
