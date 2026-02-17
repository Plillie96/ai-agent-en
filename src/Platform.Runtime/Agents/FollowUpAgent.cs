using Platform.Core.Agents;

namespace Platform.Runtime.Agents;

public sealed class FollowUpAgent : IAgent
{
    public AgentIdentity Identity { get; } = new(
        AgentId: "sales-followup",
        Name: "Follow-Up Scheduling Agent",
        Department: "Sales",
        Capabilities: ["book-meetings", "send-notifications", "update-crm"],
        RiskTier: RiskTier.Low);

    public Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken ct = default)
    {
        var meetingTime = DateTimeOffset.UtcNow.AddBusinessDays(2);

        return Task.FromResult(new AgentResult
        {
            Succeeded = true,
            Duration = TimeSpan.FromSeconds(1.5),
            Outputs = new Dictionary<string, object>
            {
                ["MeetingBooked"] = true,
                ["MeetingTime"] = meetingTime.ToString("O"),
                ["CrmUpdated"] = true,
                ["RepNotified"] = true,
                ["Summary"] = "Follow-up meeting booked, CRM updated with revised pricing and docs sent, rep notified via Slack"
            },
            Impact = new ImpactRecord
            {
                TimeSaved = TimeSpan.FromMinutes(25),
                RevenueInfluenced = context.Inputs.GetDecimal("RevisedAmount"),
                Description = "Automated meeting booking, CRM update, and rep notification"
            }
        });
    }
}

internal static class DateTimeOffsetExtensions
{
    public static DateTimeOffset AddBusinessDays(this DateTimeOffset date, int days)
    {
        var result = date;
        var added = 0;
        while (added < days)
        {
            result = result.AddDays(1);
            if (result.DayOfWeek != DayOfWeek.Saturday && result.DayOfWeek != DayOfWeek.Sunday)
                added++;
        }
        return result;
    }
}