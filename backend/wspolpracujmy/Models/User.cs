using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace wspolpracujmy.Models;

[Table("Users")]
public class User
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [Column("mail")]
    public required string Mail { get; set; }

    [Required]
    [Column("password")]
    public required string Password { get; set; }

    [Column("company_TIN")]
    public string? CompanyTin { get; set; }

    [Column("verified")]
    public bool Verified { get; set; } = false;

    [ForeignKey(nameof(CompanyTin))]
    public Company? Company { get; set; }
}
