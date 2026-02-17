namespace Platform.Core.Governance;

public interface IPolicyEngine
{
    Task<PolicyDecision> EvaluateAsync(PolicyEvaluationContext context, CancellationToken ct = default);
}

public sealed class PolicyEvaluationContext
{
    public required string AgentId { get; init; }
    public required string WorkflowInstanceId { get; init; }
    public required string StepId { get; init; }
    public required string Action { get; init; }
    public string? Department { get; init; }
    public Dictionary<string, object> Parameters { get; init; } = [];
}

public sealed class PolicyDecision
{
    public required PolicyAction Action { get; init; }
    public required string PolicyId { get; init; }
    public required string RuleId { get; init; }
    public string? Reason { get; init; }
    public DateTimeOffset EvaluatedAt { get; init; } = DateTimeOffset.UtcNow;
}
