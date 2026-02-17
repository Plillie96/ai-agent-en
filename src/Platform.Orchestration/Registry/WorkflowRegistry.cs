using Platform.Core.Workflows;

namespace Platform.Orchestration.Registry;

public sealed class WorkflowRegistry
{
    private readonly Dictionary<string, WorkflowDefinition> _workflows = new(StringComparer.OrdinalIgnoreCase);

    public void Register(WorkflowDefinition workflow)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        _workflows[workflow.WorkflowId] = workflow;
    }

    public WorkflowDefinition Resolve(string workflowId)
    {
        if (!_workflows.TryGetValue(workflowId, out var workflow))
            throw new InvalidOperationException(string.Format("Workflow '{0}' is not registered.", workflowId));
        return workflow;
    }

    public WorkflowDefinition? FindByTrigger(string eventType) =>
        _workflows.Values.FirstOrDefault(w =>
            string.Equals(w.TriggerEventType, eventType, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyCollection<WorkflowDefinition> ListAll() => _workflows.Values.ToList();
}
