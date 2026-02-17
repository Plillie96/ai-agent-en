namespace Platform.Core.Agents;

public sealed record AgentIdentity(
    string AgentId,
    string Name,
    string Department,
    string[] Capabilities,
    RiskTier RiskTier = RiskTier.Low)
{
    public DateTimeOffset RegisteredAt { get; init; } = DateTimeOffset.UtcNow;
}

public enum RiskTier
{
    Low,
    Medium,
    High,
    Critical
}
