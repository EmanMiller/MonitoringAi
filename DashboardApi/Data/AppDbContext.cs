using Microsoft.EntityFrameworkCore;

namespace DashboardApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<LogMapping> LogMappings => Set<LogMapping>();
    public DbSet<SavedQuery> SavedQueries => Set<SavedQuery>();
    public DbSet<Activity> Activities => Set<Activity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LogMapping>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Category).HasMaxLength(64);
            e.Property(x => x.Key).HasMaxLength(256);
            e.Property(x => x.Value).HasMaxLength(2048);
        });
        modelBuilder.Entity<SavedQuery>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(512);
            e.Property(x => x.QueryText).HasMaxLength(8192);
            e.Property(x => x.Category).HasMaxLength(64);
            e.Property(x => x.Tags).HasMaxLength(1024);
        });
        modelBuilder.Entity<Activity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Type).HasMaxLength(64);
            e.Property(x => x.Description).HasMaxLength(512);
            e.Property(x => x.UserId).HasMaxLength(128);
        });
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserName).IsUnique();
            e.Property(x => x.UserName).HasMaxLength(128);
            e.Property(x => x.PasswordHash).HasMaxLength(256);
            e.Property(x => x.Role).HasMaxLength(64);
            e.Property(x => x.RefreshToken).HasMaxLength(512);
        });
    }
}
