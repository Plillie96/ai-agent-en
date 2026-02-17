using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Platform.Core.Governance;

namespace Platform.Governance.Persistence;

public sealed class EfCoreAuditLog : IAuditLog
{
    private readonly GovernanceDbContext _db;

    public EfCoreAuditLog(GovernanceDbContext db)
    {
        _db = db;
    }

    public async Task RecordAsync(AuditEntry entry, CancellationToken ct = default)
    {
        _db.AuditEntries.Add(new AuditEntryEntity
        {
            EntryId = entry.EntryId,
            WorkflowInstanceId = entry.WorkflowInstanceId,
            StepId = entry.StepId,
            AgentId = entry.AgentId,
            Action = entry.Action,
            Outcome = (int)entry.Outcome,
            Timestamp = entry.Timestamp,
            PolicyId = entry.PolicyId,
            Justification = entry.Justification,
            DetailsJson = entry.Details.Count > 0 ? JsonSerializer.Serialize(entry.Details) : null,
            ApprovedBy = entry.ApprovedBy
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AuditEntry>> QueryAsync(AuditQuery query, CancellationToken ct = default)
    {
        var q = _db.AuditEntries.AsQueryable();

        if (query.WorkflowInstanceId is not null)
            q = q.Where(e => e.WorkflowInstanceId == query.WorkflowInstanceId);
        if (query.AgentId is not null)
            q = q.Where(e => e.AgentId == query.AgentId);
        if (query.From.HasValue)
            q = q.Where(e => e.Timestamp >= query.From.Value);
        if (query.To.HasValue)
            q = q.Where(e => e.Timestamp <= query.To.Value);

        var entities = await q.ToListAsync(ct);
        entities = entities
            .OrderByDescending(e => e.Timestamp)
            .Take(query.Limit)
            .ToList();

        return entities.Select(e => new AuditEntry
        {
            EntryId = e.EntryId,
            WorkflowInstanceId = e.WorkflowInstanceId,
            StepId = e.StepId,
            AgentId = e.AgentId,
            Action = e.Action,
            Outcome = (AuditOutcome)e.Outcome,
            Timestamp = e.Timestamp,
            PolicyId = e.PolicyId,
            Justification = e.Justification,
            Details = !string.IsNullOrEmpty(e.DetailsJson)
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(e.DetailsJson) ?? []
                : [],
            ApprovedBy = e.ApprovedBy
        }).ToList();
    }
}