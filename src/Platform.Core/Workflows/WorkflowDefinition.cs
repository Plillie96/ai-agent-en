namespace Platform.Core.Workflows;

public sealed class WorkflowDefinition
{
    public required string WorkflowId { get; init; }
    public required string Name { get; init; }
    public required string Department { get; init; }
    public string? Description { get; init; }
    public required List<StepDefinition> Steps { get; init; }
    public string? TriggerEventType { get; init; }
    public Dictionary<string, object> DefaultInputs { get; init; } = [];
}

public sealed class StepDefinition
{
    public required string StepId { get; init; }
    public required string AgentId { get; init; }
    public required string Name { get; init; }
    public Dictionary<string, string> InputMappings { get; init; } = [];
    public string? ConditionExpression { get; init; }
    public string? OnFailureStepId { get; init; }
    public bool RequiresApproval { get; init; }
    public TimeSpan? Timeout { get; init; }
}
