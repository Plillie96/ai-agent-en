using Platform.Core.Agents;

namespace Platform.Runtime.Agents.Finance;

public sealed class RiskFlaggingAgent : IAgent
{
    public AgentIdentity Identity { get; } = new(
        AgentId: "finance-risk-flagging",
        Name: "Financial Risk Flagging Agent",
        Department: "Finance",
        Capabilities: ["flag-risky-entries", "score-risk", "generate-variance-report"],
        RiskTier: RiskTier.Medium);

    public Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken ct = default)
    {
        return Task.FromResult(new AgentResult
        {
            Succeeded = true,
            Duration = TimeSpan.FromSeconds(3.1),
            Outputs = new Dictionary<string, object>
            {
                ["RiskyEntries"] = 3,
                ["FlaggedItems"] = new[]
                {
                    new { EntryId = "JE-9821", Risk = "High", Reason = "Unusual accrual reversal pattern" },
                    new { EntryId = "JE-9834", Risk = "Medium", Reason = "Vendor payment exceeds PO by 18%" },
                    new { EntryId = "JE-9847", Risk = "Medium", Reason = "Intercompany elimination mismatch" }
                },
                ["EbitdaImpact"] = -142000m,
                ["VarianceExplanation"] = "EBITDA variance driven by 3 flagged entries totaling $142K impact. Primary driver: accrual reversal in JE-9821."
            },
            Impact = new ImpactRecord
            {
                CostSaved = 8500m,
                TimeSaved = TimeSpan.FromHours(6),
                Description = "Automated risk flagging and board-ready variance explanation"
            }
        });
    }
}