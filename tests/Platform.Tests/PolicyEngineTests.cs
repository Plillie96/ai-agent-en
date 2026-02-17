using Microsoft.Extensions.Logging.Abstractions;
using Platform.Core.Governance;
using Platform.Governance.Engine;
using Xunit;

namespace Platform.Tests;

public class PolicyEngineTests
{
    private readonly RuleBasedPolicyEngine _engine;

    public PolicyEngineTests()
    {
        _engine = new RuleBasedPolicyEngine(NullLogger<RuleBasedPolicyEngine>.Instance);
    }

    [Fact]
    public async Task EvaluateAsync_NoMatchingRules_DefaultsToAllow()
    {
        var context = new PolicyEvaluationContext
        {
            AgentId = "agent-1",
            WorkflowInstanceId = "wf-1",
            StepId = "step-1",
            Action = "DoSomething"
        };

        var decision = await _engine.EvaluateAsync(context);

        Assert.Equal(PolicyAction.Allow, decision.Action);
    }

    [Fact]
    public async Task EvaluateAsync_ActionPattern_MatchesCorrectly()
    {
        _engine.AddPolicy(new GovernancePolicy
        {
            PolicyId = "block-transfers",
            Name = "Block Transfers",
            Scope = PolicyScope.Global,
            Rules =
            [
                new PolicyRule
                {
                    RuleId = "rule-1",
                    Description = "Block transfers",
                    ConditionExpression = "action:.*transfer.*",
                    Action = PolicyAction.Deny,
                    Severity = PolicySeverity.Critical
                }
            ]
        });

        var context = new PolicyEvaluationContext
        {
            AgentId = "agent-1",
            WorkflowInstanceId = "wf-1",
            StepId = "step-1",
            Action = "Execute money transfer"
        };

        var decision = await _engine.EvaluateAsync(context);

        Assert.Equal(PolicyAction.Deny, decision.Action);
    }

    [Fact]
    public async Task EvaluateAsync_ParameterComparison_WorksCorrectly()
    {
        _engine.AddPolicy(new GovernancePolicy
        {
            PolicyId = "high-value",
            Name = "High Value Guard",
            Scope = PolicyScope.Global,
            Rules =
            [
                new PolicyRule
                {
                    RuleId = "rule-1",
                    Description = "Require approval for amounts over 100000",
                    ConditionExpression = "param:amount:gt:100000",
                    Action = PolicyAction.RequireApproval,
                    Severity = PolicySeverity.High
                }
            ]
        });

        var context = new PolicyEvaluationContext
        {
            AgentId = "agent-1",
            WorkflowInstanceId = "wf-1",
            StepId = "step-1",
            Action = "ProcessPayment",
            Parameters = new Dictionary<string, object> { ["amount"] = 250000m }
        };

        var decision = await _engine.EvaluateAsync(context);

        Assert.Equal(PolicyAction.RequireApproval, decision.Action);
    }

    [Fact]
    public async Task EvaluateAsync_DepartmentScope_OnlyMatchesDepartment()
    {
        _engine.AddPolicy(new GovernancePolicy
        {
            PolicyId = "sales-only",
            Name = "Sales Only Policy",
            Department = "Sales",
            Scope = PolicyScope.Department,
            Rules =
            [
                new PolicyRule
                {
                    RuleId = "rule-1",
                    Description = "Audit all sales actions",
                    ConditionExpression = "always",
                    Action = PolicyAction.Deny,
                    Severity = PolicySeverity.Medium
                }
            ]
        });

        var salesContext = new PolicyEvaluationContext
        {
            AgentId = "agent-1",
            WorkflowInstanceId = "wf-1",
            StepId = "step-1",
            Action = "SalesAction",
            Department = "Sales"
        };

        var hrContext = new PolicyEvaluationContext
        {
            AgentId = "agent-1",
            WorkflowInstanceId = "wf-1",
            StepId = "step-1",
            Action = "HRAction",
            Department = "HR"
        };

        var salesDecision = await _engine.EvaluateAsync(salesContext);
        var hrDecision = await _engine.EvaluateAsync(hrContext);

        Assert.Equal(PolicyAction.Deny, salesDecision.Action);
        Assert.Equal(PolicyAction.Allow, hrDecision.Action);
    }
}