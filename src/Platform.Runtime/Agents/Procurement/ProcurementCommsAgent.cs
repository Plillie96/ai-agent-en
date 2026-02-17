using Platform.Core.Agents;

namespace Platform.Runtime.Agents.Procurement;

public sealed class ProcurementCommsAgent : IAgent
{
    public AgentIdentity Identity { get; } = new(
        AgentId: "procurement-comms",
        Name: "Procurement Communications Agent",
        Department: "Procurement",
        Capabilities: ["send-counterproposal", "handle-rejection", "escalate"],
        RiskTier: RiskTier.Low);

    public Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken ct = default)
    {
        var counterProposal = context.Inputs.TryGetValue("CounterProposal", out var cp) ? Convert.ToDouble(cp) : 5.0;

        return Task.FromResult(new AgentResult
        {
            Succeeded = true,
            Duration = TimeSpan.FromSeconds(1.0),
            Outputs = new Dictionary<string, object>
            {
                ["EmailSent"] = true,
                ["Subject"] = "Re: Annual Pricing Review - Counter Proposal",
                ["CounterProposalSent"] = counterProposal,
                ["EscalationReady"] = true,
                ["AutoEscalateOnRejection"] = true,
                ["SavingsTracked"] = true
            },
            Impact = new ImpactRecord
            {
                TimeSaved = TimeSpan.FromHours(1),
                Description = "Automated counterproposal communication to vendor"
            }
        });
    }
}