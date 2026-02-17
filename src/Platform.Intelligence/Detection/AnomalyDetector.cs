using Platform.Core.Events;

namespace Platform.Intelligence.Detection;

public sealed class AnomalyDetector
{
    private readonly List<DetectionRule> _rules = [];

    public void AddRule(DetectionRule rule) => _rules.Add(rule);

    public IReadOnlyList<DetectedAnomaly> Evaluate(SystemEvent evt)
    {
        var anomalies = new List<DetectedAnomaly>();

        foreach (var rule in _rules)
        {
            if (!string.Equals(rule.EventType, evt.EventType, StringComparison.OrdinalIgnoreCase))
                continue;

            if (rule.Condition(evt))
            {
                anomalies.Add(new DetectedAnomaly
                {
                    RuleId = rule.RuleId,
                    Description = rule.Description,
                    Severity = rule.Severity,
                    SourceEvent = evt,
                    RecommendedAction = rule.RecommendedAction
                });
            }
        }

        return anomalies;
    }
}

public sealed class DetectionRule
{
    public required string RuleId { get; init; }
    public required string EventType { get; init; }
    public required string Description { get; init; }
    public required Func<SystemEvent, bool> Condition { get; init; }
    public EventSeverity Severity { get; init; } = EventSeverity.Warning;
    public string? RecommendedAction { get; init; }
}

public sealed class DetectedAnomaly
{
    public required string RuleId { get; init; }
    public required string Description { get; init; }
    public required EventSeverity Severity { get; init; }
    public required SystemEvent SourceEvent { get; init; }
    public string? RecommendedAction { get; init; }
    public DateTimeOffset DetectedAt { get; init; } = DateTimeOffset.UtcNow;
}