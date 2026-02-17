namespace Platform.Core.Governance;

public interface IAuditLog
{
    Task RecordAsync(AuditEntry entry, CancellationToken ct = default);
    Task<IReadOnlyList<AuditEntry>> QueryAsync(AuditQuery query, CancellationToken ct = default);
}

public sealed class AuditQuery
{
    public string? WorkflowInstanceId { get; init; }
    public string? AgentId { get; init; }
    public string? Department { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public int Limit { get; init; } = 100;
}
