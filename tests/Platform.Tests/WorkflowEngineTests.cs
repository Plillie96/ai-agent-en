using Microsoft.Extensions.Logging.Abstractions;
using Platform.Core.Agents;
using Platform.Core.Governance;
using Platform.Core.Workflows;
using Platform.Governance.Audit;
using Platform.Governance.Engine;
using Platform.Orchestration.Engine;
using Platform.Orchestration.Registry;
using Xunit;

namespace Platform.Tests;

public class WorkflowEngineTests
{
    private readonly WorkflowEngine _engine;
    private readonly AgentRegistry _agents;
    private readonly InMemoryAuditLog _auditLog;
    private readonly RuleBasedPolicyEngine _policyEngine;

    public WorkflowEngineTests()
    {
        _agents = new AgentRegistry();
        _auditLog = new InMemoryAuditLog();
        _policyEngine = new RuleBasedPolicyEngine(NullLogger<RuleBasedPolicyEngine>.Instance);
        _engine = new WorkflowEngine(_agents, _policyEngine, _auditLog, NullLogger<WorkflowEngine>.Instance);
    }

    [Fact]
    public async Task ExecuteAsync_SimpleWorkflow_CompletesSuccessfully()
    {
        // Arrange
        _agents.Register(new TestAgent("agent-1", true));
        _agents.Register(new TestAgent("agent-2", true));

        var workflow = new WorkflowDefinition
        {
            WorkflowId = "test-workflow",
            Name = "Test Workflow",
            Department = "Test",
            Steps =
            [
                new StepDefinition { StepId = "step-1", AgentId = "agent-1", Name = "Step One" },
                new StepDefinition { StepId = "step-2", AgentId = "agent-2", Name = "Step Two" }
            ]
        };

        // Act
        var instance = await _engine.ExecuteAsync(workflow);

        // Assert
        Assert.Equal(WorkflowStatus.Completed, instance.Status);
        Assert.Equal(2, instance.StepExecutions.Count);
        Assert.All(instance.StepExecutions, e => Assert.Equal(StepStatus.Completed, e.Status));
    }

    [Fact]
    public async Task ExecuteAsync_StepFails_WorkflowFails()
    {
        _agents.Register(new TestAgent("agent-1", true));
        _agents.Register(new TestAgent("agent-2", false));

        var workflow = new WorkflowDefinition
        {
            WorkflowId = "fail-workflow",
            Name = "Failing Workflow",
            Department = "Test",
            Steps =
            [
                new StepDefinition { StepId = "step-1", AgentId = "agent-1", Name = "Step One" },
                new StepDefinition { StepId = "step-2", AgentId = "agent-2", Name = "Step Two" }
            ]
        };

        var instance = await _engine.ExecuteAsync(workflow);

        Assert.Equal(WorkflowStatus.Failed, instance.Status);
        Assert.NotNull(instance.FailureReason);
    }

    [Fact]
    public async Task ExecuteAsync_PolicyDeniesStep_StepFails()
    {
        _agents.Register(new TestAgent("agent-1", true));

        _policyEngine.AddPolicy(new GovernancePolicy
        {
            PolicyId = "deny-policy",
            Name = "Deny All",
            Scope = PolicyScope.Global,
            Rules =
            [
                new PolicyRule
                {
                    RuleId = "deny-all",
                    Description = "Deny everything",
                    ConditionExpression = "always",
                    Action = PolicyAction.Deny,
                    Severity = PolicySeverity.Critical
                }
            ]
        });

        var workflow = new WorkflowDefinition
        {
            WorkflowId = "denied-workflow",
            Name = "Denied Workflow",
            Department = "Test",
            Steps = [new StepDefinition { StepId = "step-1", AgentId = "agent-1", Name = "Denied Step" }]
        };

        var instance = await _engine.ExecuteAsync(workflow);

        Assert.Equal(WorkflowStatus.Failed, instance.Status);
        Assert.Contains("Denied by policy", instance.FailureReason);
    }

    [Fact]
    public async Task ExecuteAsync_AuditTrail_IsRecorded()
    {
        _agents.Register(new TestAgent("agent-1", true));

        var workflow = new WorkflowDefinition
        {
            WorkflowId = "audit-workflow",
            Name = "Audited Workflow",
            Department = "Test",
            Steps = [new StepDefinition { StepId = "step-1", AgentId = "agent-1", Name = "Audited Step" }]
        };

        await _engine.ExecuteAsync(workflow);

        var entries = _auditLog.GetAll();
        Assert.NotEmpty(entries);
        Assert.Contains(entries, e => e.AgentId == "agent-1");
    }

    [Fact]
    public async Task ExecuteAsync_ConditionalStep_SkipsWhenFalse()
    {
        _agents.Register(new TestAgent("agent-1", true));
        _agents.Register(new TestAgent("agent-2", true));

        var workflow = new WorkflowDefinition
        {
            WorkflowId = "conditional-workflow",
            Name = "Conditional Workflow",
            Department = "Test",
            Steps =
            [
                new StepDefinition { StepId = "step-1", AgentId = "agent-1", Name = "Always Runs" },
                new StepDefinition { StepId = "step-2", AgentId = "agent-2", Name = "Conditional Step", ConditionExpression = "nonExistentKey" }
            ]
        };

        var instance = await _engine.ExecuteAsync(workflow);

        Assert.Equal(WorkflowStatus.Completed, instance.Status);
        Assert.Equal(StepStatus.Completed, instance.StepExecutions[0].Status);
        Assert.Equal(StepStatus.Skipped, instance.StepExecutions[1].Status);
    }

    [Fact]
    public async Task ExecuteAsync_RequiresApproval_StepAwaitsApproval()
    {
        _agents.Register(new TestAgent("agent-1", true));

        var workflow = new WorkflowDefinition
        {
            WorkflowId = "approval-workflow",
            Name = "Approval Workflow",
            Department = "Test",
            Steps = [new StepDefinition { StepId = "step-1", AgentId = "agent-1", Name = "Needs Approval", RequiresApproval = true }]
        };

        var instance = await _engine.ExecuteAsync(workflow);

        Assert.Equal(StepStatus.AwaitingApproval, instance.StepExecutions[0].Status);
    }

    private sealed class TestAgent : IAgent
    {
        private readonly bool _succeeds;

        public TestAgent(string agentId, bool succeeds)
        {
            _succeeds = succeeds;
            Identity = new AgentIdentity(agentId, agentId, "Test", ["test"]);
        }

        public AgentIdentity Identity { get; }

        public Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken ct = default)
        {
            return Task.FromResult(new AgentResult
            {
                Succeeded = _succeeds,
                Duration = TimeSpan.FromMilliseconds(100),
                ErrorMessage = _succeeds ? null : "Simulated failure",
                Outputs = new Dictionary<string, object> { ["testOutput"] = "value" }
            });
        }
    }
}