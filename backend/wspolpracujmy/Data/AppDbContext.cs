using Microsoft.EntityFrameworkCore;
using wspolpracujmy.Models;

namespace wspolpracujmy.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Service> Services { get; set; } = null!;
    public DbSet<Company> Companies { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Match> Matches { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresEnum<StatusEnum>("status_enum");
        base.OnModelCreating(modelBuilder);
    }
}
