using System;
using Microsoft.EntityFrameworkCore;
using wspolpracujmy.Models;

namespace wspolpracujmy.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<FileEntity> Files { get; set; }
        public DbSet<GroupFile> GroupFiles { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Response> Responses { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<ProjectTag> ProjectTags { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<CalendarEvent> CalendarEvents { get; set; }
        public DbSet<MeetingType> Meeting_types { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Table names (ensure exact names from init.sql)
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Company>().ToTable("Companies");
            modelBuilder.Entity<Student>().ToTable("Students");
            modelBuilder.Entity<Group>().ToTable("Groups");
            modelBuilder.Entity<Project>().ToTable("Project");
            modelBuilder.Entity<FileEntity>().ToTable("Files");
            modelBuilder.Entity<GroupFile>().ToTable("GroupFiles");
            modelBuilder.Entity<Comment>().ToTable("Comments");
            modelBuilder.Entity<Response>().ToTable("Responses");
            modelBuilder.Entity<Tag>().ToTable("Tags");
            modelBuilder.Entity<ProjectTag>().ToTable("ProjectTags");
            modelBuilder.Entity<Notification>().ToTable("Notifications");
            modelBuilder.Entity<CalendarEvent>().ToTable("CalendarEvents");
            modelBuilder.Entity<MeetingType>().ToTable("Meeting_types");

            // Composite keys
            modelBuilder.Entity<ProjectTag>()
                .HasKey(pt => new { pt.ProjectId, pt.TagId });

            modelBuilder.Entity<GroupFile>()
                .HasKey(gf => new { gf.GroupId, gf.FileId });

            // Enum conversions: store as strings where SQL used ENUM
            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<string>();

            modelBuilder.Entity<Group>()
                .Property(g => g.IsAccepted)
                .HasConversion<string>();

            modelBuilder.Entity<Project>()
                .Property(p => p.LanguageDoc)
                .HasConversion<string>();

            modelBuilder.Entity<Notification>()
                .Property(n => n.Status)
                .HasConversion<string>();

            // Priority in DB is ENUM(1..5) — map to integer values
            modelBuilder.Entity<Project>()
                .Property(p => p.Priority)
                .HasConversion<int>();

            // Relations (per init.sql)
            modelBuilder.Entity<Company>()
                .HasOne(c => c.User)
                .WithOne(u => u.Company)
                .HasForeignKey<Company>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Student>()
                .HasOne(s => s.User)
                .WithOne(u => u.Student)
                .HasForeignKey<Student>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Student>()
                .HasOne(s => s.Group)
                .WithMany(g => g.Members)
                .HasForeignKey(s => s.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Group>()
                .HasOne(g => g.Project)
                .WithMany(p => p.Groups)
                .HasForeignKey(g => g.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Group>()
                .HasOne(g => g.Leader)
                .WithMany()
                .HasForeignKey(g => g.LeaderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Project>()
                .HasOne(p => p.Company)
                .WithMany(c => c.Projects)
                .HasForeignKey(p => p.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Project>()
                .HasOne(p => p.MeetingType)
                .WithMany()
                .HasForeignKey(p => p.MeetingTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FileEntity>()
                .HasOne(f => f.User)
                .WithMany(u => u.Files)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GroupFile>()
                .HasOne(gf => gf.Group)
                .WithMany(g => g.GroupFiles)
                .HasForeignKey(gf => gf.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GroupFile>()
                .HasOne(gf => gf.File)
                .WithMany()
                .HasForeignKey(gf => gf.FileId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Project)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Response>()
                .HasOne(r => r.Comment)
                .WithMany(c => c.Responses)
                .HasForeignKey(r => r.CommentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Response>()
                .HasOne(r => r.User)
                .WithMany(u => u.Responses)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjectTag>()
                .HasOne(pt => pt.Project)
                .WithMany(p => p.ProjectTags)
                .HasForeignKey(pt => pt.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjectTag>()
                .HasOne(pt => pt.Tag)
                .WithMany(t => t.ProjectTags)
                .HasForeignKey(pt => pt.TagId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CalendarEvent>()
                .HasOne(c => c.Group)
                .WithMany(g => g.CalendarEvents)
                .HasForeignKey(c => c.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
