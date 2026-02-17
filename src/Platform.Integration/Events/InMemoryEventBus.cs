using System.Threading.Channels;
using Platform.Core.Events;

namespace Platform.Integration.Events;

public sealed class InMemoryEventBus : IEventBus
{
    private readonly Channel<SystemEvent> _channel = Channel.CreateUnbounded<SystemEvent>(
        new UnboundedChannelOptions { SingleReader = false, SingleWriter = false });

    public async Task PublishAsync(SystemEvent evt, CancellationToken ct = default)
    {
        await _channel.Writer.WriteAsync(evt, ct);
    }

    public async IAsyncEnumerable<SystemEvent> SubscribeAsync(
        string? department = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var evt in _channel.Reader.ReadAllAsync(ct))
        {
            if (department is null || string.Equals(evt.Department, department, StringComparison.OrdinalIgnoreCase))
                yield return evt;
        }
    }
}