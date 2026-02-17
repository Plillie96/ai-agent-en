namespace Platform.Core.Governance;

public sealed class GovernancePolicy
{
    public required string PolicyId { get; init; }
    public required string Name { get; init; }
    public string? Department { get; init; }
    public required PolicyScope Scope { get; init; }
    public required List<PolicyRule> Rules { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed class PolicyRule
{
    public required string RuleId { get; init; }
    public required string Description { get; init; }
    public required string ConditionExpression { get; init; }
    public required PolicyAction Action { get; init; }
    public PolicySeverity Severity { get; init; } = PolicySeverity.Medium;
}

public enum PolicyScope
{
    Global,
    Department,
    Workflow,
    Agent
}

public enum PolicyAction
{
    Allow,
    Deny,
    RequireApproval,
    Audit,
    Alert
}

public enum PolicySeverity
{
    Low,
    Medium,
    High,
    Critical
}
