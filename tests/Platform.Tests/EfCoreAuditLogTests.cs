using Microsoft.EntityFrameworkCore;
using Platform.Core.Governance;
using Platform.Governance.Persistence;
using Xunit;

namespace Platform.Tests;

public class EfCoreAuditLogTests : IDisposable
{
    private readonly GovernanceDbContext _db;
    private readonly EfCoreAuditLog _auditLog;

    public EfCoreAuditLogTests()
    {
        var options = new DbContextOptionsBuilder<GovernanceDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        _db = new GovernanceDbContext(options);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();
        _auditLog = new EfCoreAuditLog(_db);
    }

    [Fact]
    public async Task RecordAndQuery_PersistsAndRetrieves()
    {
        var entry = new AuditEntry
        {
            WorkflowInstanceId = "wf-1",
            StepId = "step-1",
            AgentId = "agent-1",
            Action = "TestAction",
            Outcome = AuditOutcome.Allowed
        };

        await _auditLog.RecordAsync(entry);

        var results = await _auditLog.QueryAsync(new AuditQuery { WorkflowInstanceId = "wf-1" });

        Assert.Single(results);
        Assert.Equal("agent-1", results[0].AgentId);
        Assert.Equal(AuditOutcome.Allowed, results[0].Outcome);
    }

    [Fact]
    public async Task Query_FiltersByAgent()
    {
        await _auditLog.RecordAsync(new AuditEntry { WorkflowInstanceId = "wf-1", StepId = "s1", AgentId = "agent-a", Action = "A", Outcome = AuditOutcome.Allowed });
        await _auditLog.RecordAsync(new AuditEntry { WorkflowInstanceId = "wf-1", StepId = "s2", AgentId = "agent-b", Action = "B", Outcome = AuditOutcome.Denied });

        var results = await _auditLog.QueryAsync(new AuditQuery { AgentId = "agent-b" });

        Assert.Single(results);
        Assert.Equal(AuditOutcome.Denied, results[0].Outcome);
    }

    [Fact]
    public async Task Query_RespectsLimit()
    {
        for (int i = 0; i < 20; i++)
            await _auditLog.RecordAsync(new AuditEntry { WorkflowInstanceId = "wf-1", StepId = $"s{i}", AgentId = "agent-1", Action = "A", Outcome = AuditOutcome.Allowed });

        var results = await _auditLog.QueryAsync(new AuditQuery { Limit = 5 });

        Assert.Equal(5, results.Count);
    }

    public void Dispose() => _db.Dispose();
}