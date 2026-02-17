namespace Platform.Core.Workflows;

public sealed class WorkflowInstance
{
    public string InstanceId { get; init; } = Guid.NewGuid().ToString("N");
    public required string WorkflowId { get; init; }
    public WorkflowStatus Status { get; set; } = WorkflowStatus.Pending;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public Dictionary<string, object> State { get; init; } = [];
    public List<StepExecution> StepExecutions { get; init; } = [];
    public string? FailureReason { get; set; }
}

public sealed class StepExecution
{
    public required string StepId { get; init; }
    public required string AgentId { get; init; }
    public StepStatus Status { get; set; } = StepStatus.Pending;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public Dictionary<string, object> Outputs { get; init; } = [];
    public string? ErrorMessage { get; set; }
}

public enum WorkflowStatus
{
    Pending,
    Running,
    AwaitingApproval,
    Completed,
    Failed,
    Cancelled
}

public enum StepStatus
{
    Pending,
    Running,
    AwaitingApproval,
    Completed,
    Failed,
    Skipped
}
