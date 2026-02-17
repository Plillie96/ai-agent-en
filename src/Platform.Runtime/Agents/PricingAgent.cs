using Platform.Core.Agents;

namespace Platform.Runtime.Agents;

public sealed class PricingAgent : IAgent
{
    public AgentIdentity Identity { get; } = new(
        AgentId: "sales-pricing",
        Name: "Pricing Revision Agent",
        Department: "Sales",
        Capabilities: ["generate-pricing", "apply-discounts", "check-thresholds"],
        RiskTier: RiskTier.Medium);

    public Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken ct = default)
    {
        var amount = context.Inputs.TryGetValue("Amount", out var amt) ? Convert.ToDecimal(amt) : 100000m;
        var maxDiscount = 0.15m; // 15% max approved discount
        var proposedDiscount = 0.10m; // 10% discount
        var revisedAmount = amount * (1 - proposedDiscount);

        return Task.FromResult(new AgentResult
        {
            Succeeded = true,
            Duration = TimeSpan.FromSeconds(1.1),
            Outputs = new Dictionary<string, object>
            {
                ["OriginalAmount"] = amount,
                ["ProposedDiscount"] = proposedDiscount,
                ["RevisedAmount"] = revisedAmount,
                ["WithinApprovedThreshold"] = proposedDiscount <= maxDiscount,
                ["PricingJustification"] = "10% discount applied based on deal size and competitive pressure"
            },
            Impact = new ImpactRecord
            {
                RevenueInfluenced = revisedAmount,
                TimeSaved = TimeSpan.FromMinutes(30),
                Description = "Auto-generated revised pricing within approved thresholds"
            }
        });
    }
}