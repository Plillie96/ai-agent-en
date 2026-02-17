namespace Platform.Core.Events;

public interface IEventBus
{
    Task PublishAsync(SystemEvent evt, CancellationToken ct = default);
    IAsyncEnumerable<SystemEvent> SubscribeAsync(string? department = null, CancellationToken ct = default);
}
