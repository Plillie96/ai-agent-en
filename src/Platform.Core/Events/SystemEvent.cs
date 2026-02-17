namespace Platform.Core.Events;

public sealed class SystemEvent
{
    public string EventId { get; init; } = Guid.NewGuid().ToString("N");
    public required string Source { get; init; }
    public required string EventType { get; init; }
    public required string Department { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public Dictionary<string, object> Payload { get; init; } = [];
    public EventSeverity Severity { get; init; } = EventSeverity.Info;
}

public enum EventSeverity
{
    Info,
    Warning,
    Anomaly,
    Critical
}
