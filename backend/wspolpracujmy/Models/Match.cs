using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace wspolpracujmy.Models;

[Table("Matches")]
public class Match
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [Column("company_TIN")]
    public required string CompanyTin { get; set; }

    [Required]
    [Column("matched_company_TIN")]
    public required string MatchedCompanyTin { get; set; }

    [Required]
    [Column("status", TypeName = "status_enum")]
    public StatusEnum Status { get; set; } = StatusEnum.pending;

    [Required]
    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [ForeignKey(nameof(CompanyTin))]
    [InverseProperty("MatchesInitiated")]
    public Company? Company { get; set; }

    [ForeignKey(nameof(MatchedCompanyTin))]
    [InverseProperty("MatchesReceived")]
    public Company? MatchedCompany { get; set; }
}
