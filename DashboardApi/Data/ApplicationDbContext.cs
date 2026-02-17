using Microsoft.EntityFrameworkCore;

namespace DashboardApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Dashboard> Dashboards { get; set; }
    public DbSet<Query> Queries { get; set; }
    public DbSet<Activity> Activities { get; set; }
    public DbSet<ChatHistory> ChatHistory { get; set; }
    public DbSet<LogMapping> LogMappings { get; set; }
    public DbSet<SavedQuery> SavedQueries { get; set; }
    public DbSet<UserPreferences> UserPreferences { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // User
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(u => u.Username).IsUnique();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Username).HasMaxLength(256);
            e.Property(u => u.Email).HasMaxLength(256);
            e.Property(u => u.PasswordHash).HasMaxLength(256);
            e.Property(u => u.Role).HasMaxLength(64);
            e.Property(u => u.RefreshToken).HasMaxLength(512);
        });

        // Dashboard
        modelBuilder.Entity<Dashboard>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(d => d.Name).HasMaxLength(50);
            e.HasOne<User>().WithMany().HasForeignKey(d => d.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // Query
        modelBuilder.Entity<Query>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(q => q.Category);
            e.Property(q => q.Key).HasMaxLength(200);
            e.Property(q => q.Value).HasColumnType("text");
            e.HasOne<User>().WithMany().HasForeignKey(q => q.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        });

        // Activity
        modelBuilder.Entity<Activity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(a => a.Timestamp);
            e.Property(a => a.Type).HasMaxLength(64);
            e.Property(a => a.Description).HasMaxLength(1024);
            e.HasOne<User>().WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.SetNull);
        });

        // ChatHistory
        modelBuilder.Entity<ChatHistory>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(ch => ch.ConversationId);
            e.Property(ch => ch.Role).HasMaxLength(32);
            e.Property(ch => ch.Content).HasColumnType("text");
            e.HasOne<User>().WithMany().HasForeignKey(ch => ch.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // LogMapping (existing)
        modelBuilder.Entity<LogMapping>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Category).HasMaxLength(64);
            e.Property(x => x.Key).HasMaxLength(256);
            e.Property(x => x.Value).HasMaxLength(2048);
        });

        // SavedQuery (existing)
        modelBuilder.Entity<SavedQuery>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(512);
            e.Property(x => x.QueryText).HasMaxLength(8192);
            e.Property(x => x.Category).HasMaxLength(64);
            e.Property(x => x.Tags).HasMaxLength(1024);
        });

        // UserPreferences (onboarding)
        modelBuilder.Entity<UserPreferences>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserId).IsUnique();
            e.Property(x => x.UserId).HasMaxLength(128);
            e.Property(x => x.SelectedInterestsJson).HasMaxLength(2048);
        });
    }
}
