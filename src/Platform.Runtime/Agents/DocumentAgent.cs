using Platform.Core.Agents;

namespace Platform.Runtime.Agents;

public sealed class DocumentAgent : IAgent
{
    public AgentIdentity Identity { get; } = new(
        AgentId: "sales-documents",
        Name: "Document Dispatch Agent",
        Department: "Sales",
        Capabilities: ["send-security-docs", "generate-proposals", "attach-compliance"],
        RiskTier: RiskTier.Low);

    public Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken ct = default)
    {
        return Task.FromResult(new AgentResult
        {
            Succeeded = true,
            Duration = TimeSpan.FromSeconds(0.8),
            Outputs = new Dictionary<string, object>
            {
                ["DocumentsSent"] = new[] { "SOC2-Report-2024.pdf", "Security-Whitepaper.pdf", "Data-Processing-Agreement.pdf" },
                ["DeliveryMethod"] = "email",
                ["Recipient"] = context.Inputs.GetValueOrDefault("ContactEmail", "prospect@company.com")!,
                ["DocumentsSentAt"] = DateTimeOffset.UtcNow.ToString("O")
            },
            Impact = new ImpactRecord
            {
                TimeSaved = TimeSpan.FromMinutes(20),
                Description = "Automatically dispatched security documentation"
            }
        });
    }
}