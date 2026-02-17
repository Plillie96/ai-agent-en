using Microsoft.EntityFrameworkCore;

namespace Platform.Governance.Persistence;

public sealed class GovernanceDbContext : DbContext
{
    public GovernanceDbContext(DbContextOptions<GovernanceDbContext> options) : base(options) { }

    public DbSet<AuditEntryEntity> AuditEntries => Set<AuditEntryEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditEntryEntity>(e =>
        {
            e.HasKey(x => x.EntryId);
            e.HasIndex(x => x.WorkflowInstanceId);
            e.HasIndex(x => x.AgentId);
            e.HasIndex(x => x.Timestamp);
        });
    }
}