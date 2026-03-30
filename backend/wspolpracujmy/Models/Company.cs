using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace wspolpracujmy.Models;

[Table("Companies")]
public class Company
{
    [Key]
    [Column("TIN")]
    public required string Tin { get; set; }

    [Required]
    [Column("name")]
    public required string Name { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("website")]
    public string? Website { get; set; }

    [Column("contact_email")]
    public string? ContactEmail { get; set; }

    [Column("service_id")]
    public int? ServiceId { get; set; }

    [Column("offer_id")]
    public int? OfferId { get; set; }

    [Column("location")]
    public string? Location { get; set; }

    [Required]
    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [ForeignKey(nameof(ServiceId))]
    [InverseProperty("CompaniesAsService")]
    public Service? Service { get; set; }

    [ForeignKey(nameof(OfferId))]
    [InverseProperty("CompaniesAsOffer")]
    public Service? Offer { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();

    [InverseProperty(nameof(Match.Company))]
    public ICollection<Match> MatchesInitiated { get; set; } = new List<Match>();

    [InverseProperty(nameof(Match.MatchedCompany))]
    public ICollection<Match> MatchesReceived { get; set; } = new List<Match>();
}
