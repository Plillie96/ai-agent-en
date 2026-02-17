using Microsoft.Extensions.Logging.Abstractions;
using Platform.Core.Agents;
using Platform.Core.Governance;
using Platform.Core.Workflows;
using Platform.Governance.Audit;
using Platform.Governance.Engine;
using Platform.Orchestration.Engine;
using Platform.Orchestration.Registry;
using Platform.Runtime.Agents.Finance;
using Platform.Runtime.Agents.HR;
using Platform.Runtime.Agents.IT;
using Platform.Runtime.Agents.Legal;
using Platform.Runtime.Agents.Procurement;
using Platform.Runtime.Setup;
using Xunit;

namespace Platform.Tests;

public class DepartmentWorkflowTests
{
    private readonly WorkflowEngine _engine;
    private readonly AgentRegistry _agents;
    private readonly WorkflowRegistry _workflows;

    public DepartmentWorkflowTests()
    {
        _agents = PlatformBootstrap.ConfigureAgents();
        _workflows = PlatformBootstrap.ConfigureWorkflows();
        var auditLog = new InMemoryAuditLog();
        var policyEngine = PlatformBootstrap.ConfigurePolicies(NullLogger<RuleBasedPolicyEngine>.Instance);
        _engine = new WorkflowEngine(_agents, policyEngine, auditLog, NullLogger<WorkflowEngine>.Instance);
    }

    [Fact]
    public async Task SalesStalledDealRecovery_CompletesAllSteps()
    {
        var workflow = _workflows.Resolve("sales-stalled-deal-recovery");
        var inputs = new Dictionary<string, object>
        {
            ["dealId"] = "OPP-48291",
            ["daysStalled"] = 11,
            ["amount"] = 480000m
        };

        var instance = await _engine.ExecuteAsync(workflow, inputs);

        Assert.Equal(WorkflowStatus.Completed, instance.Status);
        Assert.Equal(4, instance.StepExecutions.Count);
        Assert.All(instance.StepExecutions, e => Assert.Equal(StepStatus.Completed, e.Status));
    }

    [Fact]
    public async Task FinanceReconciliation_SecondStepRequiresApproval()
    {
        var workflow = _workflows.Resolve("finance-month-end-reconciliation");

        var instance = await _engine.ExecuteAsync(workflow);

        Assert.Equal(2, instance.StepExecutions.Count);
        Assert.Equal(StepStatus.Completed, instance.StepExecutions[0].Status);
        Assert.Equal(StepStatus.AwaitingApproval, instance.StepExecutions[1].Status);
    }

    [Fact]
    public async Task LegalContractReview_CompletesClassificationAndRedline()
    {
        var workflow = _workflows.Resolve("legal-contract-review");
        var inputs = new Dictionary<string, object>
        {
            ["contractType"] = "SaaS Subscription",
            ["contractId"] = "CTR-9921"
        };

        var instance = await _engine.ExecuteAsync(workflow, inputs);

        Assert.Equal(2, instance.StepExecutions.Count);
        Assert.Equal(StepStatus.Completed, instance.StepExecutions[0].Status);
    }

    [Fact]
    public async Task ITSecurityResponse_ExecutesBothSteps()
    {
        var workflow = _workflows.Resolve("it-security-incident-response");
        var inputs = new Dictionary<string, object>
        {
            ["userId"] = "user-4521",
            ["anomalyScore"] = 85.0
        };

        var instance = await _engine.ExecuteAsync(workflow, inputs);

        Assert.Equal(2, instance.StepExecutions.Count);
        Assert.Equal(StepStatus.Completed, instance.StepExecutions[0].Status);
        Assert.Equal(StepStatus.Completed, instance.StepExecutions[1].Status);
    }

    [Fact]
    public async Task ProcurementNegotiation_GeneratesCounterproposal()
    {
        var workflow = _workflows.Resolve("procurement-vendor-negotiation");
        var inputs = new Dictionary<string, object>
        {
            ["vendorId"] = "VENDOR-AWS",
            ["priceIncrease"] = 9.0
        };

        var instance = await _engine.ExecuteAsync(workflow, inputs);

        Assert.Equal(2, instance.StepExecutions.Count);
        Assert.Equal(StepStatus.Completed, instance.StepExecutions[0].Status);
    }

    [Fact]
    public async Task HRAttritionPrevention_DetectsAndActsOnRisk()
    {
        var workflow = _workflows.Resolve("hr-attrition-prevention");
        var inputs = new Dictionary<string, object>
        {
            ["teamId"] = "eng-platform"
        };

        var instance = await _engine.ExecuteAsync(workflow, inputs);

        Assert.Equal(2, instance.StepExecutions.Count);
        Assert.Equal(StepStatus.Completed, instance.StepExecutions[0].Status);
        // Second step requires approval per policy
        Assert.Equal(StepStatus.AwaitingApproval, instance.StepExecutions[1].Status);
    }

    [Fact]
    public void AllSixWorkflowsAreRegistered()
    {
        var all = _workflows.ListAll();
        Assert.Equal(6, all.Count);
    }

    [Fact]
    public void AllFourteenAgentsAreRegistered()
    {
        var all = _agents.ListAll();
        Assert.Equal(14, all.Count);
    }
}