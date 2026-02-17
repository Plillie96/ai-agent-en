using System.Collections.Concurrent;
using Platform.Core.Governance;

namespace Platform.Governance.Audit;

public sealed class InMemoryAuditLog : IAuditLog
{
    private readonly ConcurrentBag<AuditEntry> _entries = [];

    public Task RecordAsync(AuditEntry entry, CancellationToken ct = default)
    {
        _entries.Add(entry);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AuditEntry>> QueryAsync(AuditQuery query, CancellationToken ct = default)
    {
        var results = _entries.AsEnumerable();

        if (query.WorkflowInstanceId is not null)
            results = results.Where(e => e.WorkflowInstanceId == query.WorkflowInstanceId);
        if (query.AgentId is not null)
            results = results.Where(e => e.AgentId == query.AgentId);
        if (query.From.HasValue)
            results = results.Where(e => e.Timestamp >= query.From.Value);
        if (query.To.HasValue)
            results = results.Where(e => e.Timestamp <= query.To.Value);

        IReadOnlyList<AuditEntry> list = results
            .OrderByDescending(e => e.Timestamp)
            .Take(query.Limit)
            .ToList();

        return Task.FromResult(list);
    }

    public IReadOnlyCollection<AuditEntry> GetAll() => [.. _entries];
}