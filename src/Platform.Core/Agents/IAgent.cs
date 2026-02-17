namespace Platform.Core.Agents;

public interface IAgent
{
    AgentIdentity Identity { get; }
    Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken ct = default);
}

public sealed class AgentContext
{
    public required string WorkflowInstanceId { get; init; }
    public required string StepId { get; init; }
    public Dictionary<string, object> Inputs { get; init; } = [];
    public Dictionary<string, object> SharedState { get; init; } = [];
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class AgentResult
{
    public required bool Succeeded { get; init; }
    public Dictionary<string, object> Outputs { get; init; } = [];
    public string? ErrorMessage { get; init; }
    public TimeSpan Duration { get; init; }
    public ImpactRecord? Impact { get; init; }
}

public sealed class ImpactRecord
{
    public decimal? CostSaved { get; init; }
    public decimal? RevenueInfluenced { get; init; }
    public TimeSpan? TimeSaved { get; init; }
    public string? Description { get; init; }
}
