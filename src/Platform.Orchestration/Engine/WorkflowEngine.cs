using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Platform.Core.Agents;
using Platform.Core.Governance;
using Platform.Core.Workflows;
using Platform.Orchestration.Registry;

namespace Platform.Orchestration.Engine;

public sealed class WorkflowEngine
{
    private readonly AgentRegistry _agents;
    private readonly IPolicyEngine _policyEngine;
    private readonly IAuditLog _auditLog;
    private readonly ILogger<WorkflowEngine> _logger;

    public WorkflowEngine(AgentRegistry agents, IPolicyEngine policyEngine, IAuditLog auditLog, ILogger<WorkflowEngine> logger)
    {
        _agents = agents;
        _policyEngine = policyEngine;
        _auditLog = auditLog;
        _logger = logger;
    }

    public async Task<WorkflowInstance> ExecuteAsync(WorkflowDefinition workflow, Dictionary<string, object>? inputs = null, CancellationToken ct = default)
    {
        var instance = new WorkflowInstance
        {
            WorkflowId = workflow.WorkflowId,
            Status = WorkflowStatus.Running,
            State = inputs != null ? new Dictionary<string, object>(inputs) : []
        };

        _logger.LogInformation("Starting workflow {WorkflowId} instance {InstanceId}", workflow.WorkflowId, instance.InstanceId);

        try
        {
            foreach (var step in workflow.Steps)
            {
                ct.ThrowIfCancellationRequested();

                if (!EvaluateCondition(step, instance))
                {
                    instance.StepExecutions.Add(new StepExecution { StepId = step.StepId, AgentId = step.AgentId, Status = StepStatus.Skipped });
                    continue;
                }

                var execution = await ExecuteStepAsync(instance, step, workflow, ct);
                instance.StepExecutions.Add(execution);

                if (execution.Status == StepStatus.Failed)
                {
                    if (step.OnFailureStepId is not null)
                    {
                        _logger.LogWarning("Step {StepId} failed, routing to {FailureStep}", step.StepId, step.OnFailureStepId);
                        continue;
                    }
                    instance.Status = WorkflowStatus.Failed;
                    instance.FailureReason = execution.ErrorMessage;
                    break;
                }

                foreach (var kvp in execution.Outputs)
                    instance.State[kvp.Key] = kvp.Value;
            }

            if (instance.Status == WorkflowStatus.Running)
                instance.Status = WorkflowStatus.Completed;
        }
        catch (OperationCanceledException)
        {
            instance.Status = WorkflowStatus.Cancelled;
        }
        catch (Exception ex)
        {
            instance.Status = WorkflowStatus.Failed;
            instance.FailureReason = ex.Message;
            _logger.LogError(ex, "Workflow {InstanceId} failed unexpectedly", instance.InstanceId);
        }

        instance.CompletedAt = DateTimeOffset.UtcNow;
        _logger.LogInformation("Workflow {InstanceId} completed with status {Status}", instance.InstanceId, instance.Status);
        return instance;
    }

    private async Task<StepExecution> ExecuteStepAsync(WorkflowInstance instance, StepDefinition step, WorkflowDefinition workflow, CancellationToken ct)
    {
        var execution = new StepExecution { StepId = step.StepId, AgentId = step.AgentId, StartedAt = DateTimeOffset.UtcNow };
        _logger.LogInformation("Executing step {StepId} with agent {AgentId}", step.StepId, step.AgentId);

        var policyContext = new PolicyEvaluationContext
        {
            AgentId = step.AgentId,
            WorkflowInstanceId = instance.InstanceId,
            StepId = step.StepId,
            Action = step.Name,
            Department = workflow.Department,
            Parameters = instance.State
        };

        var decision = await _policyEngine.EvaluateAsync(policyContext, ct);

        await _auditLog.RecordAsync(new AuditEntry
        {
            WorkflowInstanceId = instance.InstanceId,
            StepId = step.StepId,
            AgentId = step.AgentId,
            Action = step.Name,
            Outcome = MapDecisionToOutcome(decision.Action),
            PolicyId = decision.PolicyId,
            Justification = decision.Reason
        }, ct);

        if (decision.Action == PolicyAction.Deny)
        {
            execution.Status = StepStatus.Failed;
            execution.ErrorMessage = string.Format("Denied by policy {0}: {1}", decision.PolicyId, decision.Reason);
            execution.CompletedAt = DateTimeOffset.UtcNow;
            return execution;
        }

        if (decision.Action == PolicyAction.RequireApproval || step.RequiresApproval)
        {
            execution.Status = StepStatus.AwaitingApproval;
            _logger.LogInformation("Step {StepId} requires human approval", step.StepId);
            execution.CompletedAt = DateTimeOffset.UtcNow;
            return execution;
        }

        var agent = _agents.Resolve(step.AgentId);
        var context = new AgentContext
        {
            WorkflowInstanceId = instance.InstanceId,
            StepId = step.StepId,
            Inputs = ResolveInputs(step, instance),
            SharedState = instance.State
        };

        var sw = Stopwatch.StartNew();
        try
        {
            using var timeoutCts = step.Timeout.HasValue ? CancellationTokenSource.CreateLinkedTokenSource(ct) : null;
            timeoutCts?.CancelAfter(step.Timeout!.Value);

            var result = await agent.ExecuteAsync(context, timeoutCts?.Token ?? ct);
            sw.Stop();

            execution.Status = result.Succeeded ? StepStatus.Completed : StepStatus.Failed;
            execution.ErrorMessage = result.ErrorMessage;

            foreach (var kvp in result.Outputs)
                execution.Outputs[kvp.Key] = kvp.Value;

            await _auditLog.RecordAsync(new AuditEntry
            {
                WorkflowInstanceId = instance.InstanceId,
                StepId = step.StepId,
                AgentId = step.AgentId,
                Action = string.Format("{0}:completed", step.Name),
                Outcome = result.Succeeded ? AuditOutcome.Allowed : AuditOutcome.Denied,
                Details = new Dictionary<string, object> { ["durationMs"] = sw.ElapsedMilliseconds, ["succeeded"] = result.Succeeded }
            }, ct);
        }
        catch (OperationCanceledException) when (step.Timeout.HasValue)
        {
            execution.Status = StepStatus.Failed;
            execution.ErrorMessage = string.Format("Step timed out after {0}", step.Timeout.Value);
        }

        execution.CompletedAt = DateTimeOffset.UtcNow;
        return execution;
    }

    private static Dictionary<string, object> ResolveInputs(StepDefinition step, WorkflowInstance instance)
    {
        var inputs = new Dictionary<string, object>();
        foreach (var mapping in step.InputMappings)
        {
            if (instance.State.TryGetValue(mapping.Value, out var value))
                inputs[mapping.Key] = value;
        }
        return inputs;
    }

    private static bool EvaluateCondition(StepDefinition step, WorkflowInstance instance)
    {
        if (string.IsNullOrEmpty(step.ConditionExpression)) return true;
        if (instance.State.TryGetValue(step.ConditionExpression, out var value))
        {
            return value switch { bool b => b, string s => !string.IsNullOrEmpty(s), null => false, _ => true };
        }
        return false;
    }

    private static AuditOutcome MapDecisionToOutcome(PolicyAction action) => action switch
    {
        PolicyAction.Allow => AuditOutcome.Allowed,
        PolicyAction.Deny => AuditOutcome.Denied,
        PolicyAction.RequireApproval => AuditOutcome.EscalatedToHuman,
        PolicyAction.Audit => AuditOutcome.AuditOnly,
        PolicyAction.Alert => AuditOutcome.AuditOnly,
        _ => AuditOutcome.AuditOnly
    };
}