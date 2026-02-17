using Platform.Core.Agents;

namespace Platform.Runtime.Agents.HR;

public sealed class AttritionDetectionAgent : IAgent
{
    public AgentIdentity Identity { get; } = new(
        AgentId: "hr-attrition-detection",
        Name: "Attrition Detection Agent",
        Department: "HR",
        Capabilities: ["detect-sentiment", "analyze-attrition-risk", "compare-compensation"],
        RiskTier: RiskTier.Medium);

    public Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken ct = default)
    {
        var teamId = context.Inputs.GetString("TeamId", "eng-platform");

        return Task.FromResult(new AgentResult
        {
            Succeeded = true,
            Duration = TimeSpan.FromSeconds(3.0),
            Outputs = new Dictionary<string, object>
            {
                ["TeamId"] = teamId,
                ["AttritionProbability"] = 0.68,
                ["SentimentScore"] = 3.2,
                ["SentimentTrend"] = "Declining (-1.4 over 90 days)",
                ["CompVsMarket"] = "12% below median for Sr. Engineers",
                ["AtRiskEmployees"] = 4,
                ["TopFactors"] = new[] { "Below-market compensation", "Manager sentiment drop", "Reduced PR activity", "Increased LinkedIn activity" }
            },
            Impact = new ImpactRecord
            {
                TimeSaved = TimeSpan.FromHours(5),
                CostSaved = 25000m,
                Description = "Early attrition detection preventing costly replacement (avg $180K per senior engineer)"
            }
        });
    }
}