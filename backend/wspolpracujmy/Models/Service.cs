using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace wspolpracujmy.Models;

[Table("Services")]
public class Service
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [Column("name")]
    public required string Name { get; set; }

    [InverseProperty(nameof(Company.Service))]
    public ICollection<Company> CompaniesAsService { get; set; } = new List<Company>();

    [InverseProperty(nameof(Company.Offer))]
    public ICollection<Company> CompaniesAsOffer { get; set; } = new List<Company>();
}
