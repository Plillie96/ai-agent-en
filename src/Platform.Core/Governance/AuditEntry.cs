namespace Platform.Core.Governance;

public sealed class AuditEntry
{
    public string EntryId { get; init; } = Guid.NewGuid().ToString("N");
    public required string WorkflowInstanceId { get; init; }
    public required string StepId { get; init; }
    public required string AgentId { get; init; }
    public required string Action { get; init; }
    public required AuditOutcome Outcome { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public string? PolicyId { get; init; }
    public string? Justification { get; init; }
    public Dictionary<string, object> Details { get; init; } = [];
    public string? ApprovedBy { get; init; }
}

public enum AuditOutcome
{
    Allowed,
    Denied,
    EscalatedToHuman,
    ApprovedByHuman,
    AutoApproved,
    AuditOnly
}
