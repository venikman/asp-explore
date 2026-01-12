using Microsoft.EntityFrameworkCore;

namespace AspireDemo.Data.Analytics;

public class AnalyticsDbContext : DbContext
{
    public AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options)
        : base(options)
    {
    }

    public DbSet<PageView> PageViews => Set<PageView>();
    public DbSet<UserEvent> UserEvents => Set<UserEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PageView>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Path).HasMaxLength(500).IsRequired();
            entity.Property(e => e.UserAgent).HasMaxLength(1000);
            entity.HasIndex(e => e.Timestamp);
        });

        modelBuilder.Entity<UserEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Properties).HasColumnType("jsonb");
            entity.HasIndex(e => new { e.EventName, e.Timestamp });
        });
    }
}

public class PageView
{
    public int Id { get; set; }
    public string Path { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class UserEvent
{
    public int Id { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? Properties { get; set; } // JSON
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
