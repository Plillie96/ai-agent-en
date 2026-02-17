using Platform.Core.Agents;

namespace Platform.Runtime.Agents.IT;

public sealed class TicketingAgent : IAgent
{
    public AgentIdentity Identity { get; } = new(
        AgentId: "it-ticketing",
        Name: "IT Ticketing Agent",
        Department: "IT",
        Capabilities: ["create-ticket", "notify-ciso", "track-remediation"],
        RiskTier: RiskTier.Low);

    public Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken ct = default)
    {
        var ticketId = $"INC{Random.Shared.Next(100000, 999999)}";

        return Task.FromResult(new AgentResult
        {
            Succeeded = true,
            Duration = TimeSpan.FromSeconds(1.2),
            Outputs = new Dictionary<string, object>
            {
                ["TicketId"] = ticketId,
                ["TicketSystem"] = "ServiceNow",
                ["Priority"] = "P1",
                ["CisoNotified"] = true,
                ["DashboardUpdated"] = true,
                ["RemediationSteps"] = new[]
                {
                    "1. Review forensic report",
                    "2. Validate access restriction",
                    "3. Contact user for verification",
                    "4. Escalate if confirmed breach"
                }
            },
            Impact = new ImpactRecord
            {
                TimeSaved = TimeSpan.FromMinutes(45),
                Description = "Automated ServiceNow ticket creation and CISO notification"
            }
        });
    }
}