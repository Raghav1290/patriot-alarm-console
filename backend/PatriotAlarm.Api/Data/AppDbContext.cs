using Microsoft.EntityFrameworkCore;
using PatriotAlarm.Api.Domain;

namespace PatriotAlarm.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<AlarmEvent> AlarmEvents => Set<AlarmEvent>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Site>(e =>
        {
            e.HasIndex(s => s.AccountNumber).IsUnique();
        });

        modelBuilder.Entity<AlarmEvent>(e =>
        {
            e.Property(a => a.Priority).HasConversion<string>();
            e.Property(a => a.Status).HasConversion<string>();
            e.HasIndex(a => a.Status);
            e.HasIndex(a => a.ReceivedAtUtc);
            e.HasOne(a => a.Site).WithMany().HasForeignKey(a => a.SiteId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Job>(e =>
        {
            e.Property(j => j.Status).HasConversion<string>();
            e.HasIndex(j => j.AlarmEventId).IsUnique();
            e.HasOne(j => j.AlarmEvent).WithOne(a => a.Job).HasForeignKey<Job>(j => j.AlarmEventId);
        });

        modelBuilder.Entity<AuditEntry>(e =>
        {
            e.HasIndex(a => a.AtUtc);
        });
    }
}
