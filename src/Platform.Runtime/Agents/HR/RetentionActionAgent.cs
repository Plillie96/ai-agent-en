using Platform.Core.Agents;

namespace Platform.Runtime.Agents.HR;

public sealed class RetentionActionAgent : IAgent
{
    public AgentIdentity Identity { get; } = new(
        AgentId: "hr-retention-action",
        Name: "Retention Action Agent",
        Department: "HR",
        Capabilities: ["recommend-retention", "schedule-checkins", "track-kpi"],
        RiskTier: RiskTier.Medium);

    public Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken ct = default)
    {
        var atRiskCount = context.Inputs.TryGetValue("AtRiskEmployees", out var ar) ? Convert.ToInt32(ar) : 0;

        return Task.FromResult(new AgentResult
        {
            Succeeded = true,
            Duration = TimeSpan.FromSeconds(2.0),
            Outputs = new Dictionary<string, object>
            {
                ["RetentionPackages"] = new[]
                {
                    new { Employee = "EMP-4021", Package = "Equity refresh + title adjustment", EstCost = 45000m },
                    new { Employee = "EMP-4033", Package = "Spot bonus + project lead role", EstCost = 25000m },
                    new { Employee = "EMP-4047", Package = "Comp adjustment to market median", EstCost = 18000m },
                    new { Employee = "EMP-4052", Package = "Flexible schedule + learning budget", EstCost = 8000m }
                },
                ["ManagerCheckinsScheduled"] = atRiskCount,
                ["TotalRetentionBudget"] = 96000m,
                ["ProjectedSavingsVsReplacement"] = 720000m - 96000m,
                ["KpiTracking"] = new[] { "30-day sentiment rescore", "60-day attrition probability", "90-day retention confirmation" }
            },
            Impact = new ImpactRecord
            {
                CostSaved = 624000m,
                TimeSaved = TimeSpan.FromHours(10),
                Description = "Proactive retention packages preventing $720K replacement cost at $96K investment"
            }
        });
    }
}