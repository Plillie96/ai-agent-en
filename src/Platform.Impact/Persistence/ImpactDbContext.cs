using Microsoft.EntityFrameworkCore;

namespace Platform.Impact.Persistence;

public sealed class ImpactDbContext : DbContext
{
    public ImpactDbContext(DbContextOptions<ImpactDbContext> options) : base(options) { }

    public DbSet<ImpactMetricsEntity> ImpactMetrics => Set<ImpactMetricsEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ImpactMetricsEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Department);
            e.HasIndex(x => x.Timestamp);
            e.HasIndex(x => x.WorkflowInstanceId);
        });
    }
}