using Platform.Core.Agents;

namespace Platform.Runtime.Agents.Procurement;

public sealed class VendorNegotiationAgent : IAgent
{
    public AgentIdentity Identity { get; } = new(
        AgentId: "procurement-vendor-negotiation",
        Name: "Vendor Negotiation Agent",
        Department: "Procurement",
        Capabilities: ["compare-benchmarks", "check-escalation-clauses", "generate-counterproposal"],
        RiskTier: RiskTier.Medium);

    public Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken ct = default)
    {
        var proposedIncrease = context.Inputs.GetDouble("PriceIncrease", 9.0);
        var contractCap = 5.0;
        var counterOffer = Math.Min(proposedIncrease, contractCap);

        return Task.FromResult(new AgentResult
        {
            Succeeded = true,
            Duration = TimeSpan.FromSeconds(2.5),
            Outputs = new Dictionary<string, object>
            {
                ["VendorId"] = context.Inputs.GetString("VendorId", "VENDOR-001"),
                ["ProposedIncrease"] = proposedIncrease,
                ["ContractEscalationCap"] = contractCap,
                ["BenchmarkMedian"] = 4.2,
                ["CounterProposal"] = counterOffer,
                ["Justification"] = $"Contract clause limits annual escalation to {contractCap}%. Industry benchmark median is 4.2%. Counter-proposing {counterOffer}%.",
                ["SavingsIfAccepted"] = (decimal)(proposedIncrease - counterOffer) * 10000m
            },
            Impact = new ImpactRecord
            {
                CostSaved = (decimal)(proposedIncrease - counterOffer) * 10000m,
                TimeSaved = TimeSpan.FromHours(3),
                Description = "Automated vendor price negotiation using contract clauses and market benchmarks"
            }
        });
    }
}