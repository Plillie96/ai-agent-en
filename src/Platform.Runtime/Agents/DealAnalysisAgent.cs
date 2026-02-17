using Platform.Core.Agents;

namespace Platform.Runtime.Agents;

public sealed class DealAnalysisAgent : IAgent
{
    public AgentIdentity Identity { get; } = new(
        AgentId: "sales-deal-analysis",
        Name: "Deal Analysis Agent",
        Department: "Sales",
        Capabilities: ["analyze-opportunity", "detect-stalled-deals", "read-transcripts"],
        RiskTier: RiskTier.Low);

    public Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken ct = default)
    {
        // Simulates: detecting a stalled deal, reading transcript, identifying objections
        var daysStalled = context.Inputs.TryGetValue("DaysStalled", out var ds) ? Convert.ToInt32(ds) : 0;
        var amount = context.Inputs.TryGetValue("Amount", out var amt) ? Convert.ToDecimal(amt) : 0m;

        return Task.FromResult(new AgentResult
        {
            Succeeded = true,
            Duration = TimeSpan.FromSeconds(2.3),
            Outputs = new Dictionary<string, object>
            {
                ["DealId"] = context.Inputs.GetValueOrDefault("DealId", "OPP-001")!,
                ["DaysStalled"] = daysStalled,
                ["Amount"] = amount,
                ["Objections"] = new[] { "pricing", "missing-security-doc" },
                ["RiskLevel"] = daysStalled > 14 ? "High" : "Medium",
                ["TranscriptAnalysis"] = "Prospect raised pricing concern and requested SOC2 documentation"
            },
            Impact = new ImpactRecord
            {
                TimeSaved = TimeSpan.FromMinutes(45),
                Description = "Automated deal analysis that would require manual CRM review and transcript reading"
            }
        });
    }
}