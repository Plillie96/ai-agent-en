using Platform.Core.Agents;

namespace Platform.Runtime.Agents.IT;

public sealed class SecurityResponseAgent : IAgent
{
    public AgentIdentity Identity { get; } = new(
        AgentId: "it-security-response",
        Name: "Security Incident Response Agent",
        Department: "IT",
        Capabilities: ["correlate-logs", "score-risk", "restrict-access", "generate-forensics"],
        RiskTier: RiskTier.Critical);

    public Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken ct = default)
    {
        var userId = context.Inputs.GetString("UserId", "user-unknown");
        var riskScore = context.Inputs.GetDouble("AnomalyScore", 75.0);

        return Task.FromResult(new AgentResult
        {
            Succeeded = true,
            Duration = TimeSpan.FromSeconds(1.8),
            Outputs = new Dictionary<string, object>
            {
                ["UserId"] = userId,
                ["RiskScore"] = riskScore,
                ["CorrelatedEvents"] = 14,
                ["AccessRestricted"] = riskScore > 70.0,
                ["RestrictionType"] = riskScore > 70.0 ? "TemporarySuspend" : "MonitorOnly",
                ["ForensicReportId"] = Guid.NewGuid().ToString("N"),
                ["AffectedSystems"] = new[] { "Azure AD", "SharePoint", "GitHub Enterprise" },
                ["BehaviorPattern"] = "Unusual off-hours access from new geo-location with bulk file download"
            },
            Impact = new ImpactRecord
            {
                TimeSaved = TimeSpan.FromHours(2),
                CostSaved = 5000m,
                Description = "Automated security incident correlation, access restriction, and forensic report generation"
            }
        });
    }
}