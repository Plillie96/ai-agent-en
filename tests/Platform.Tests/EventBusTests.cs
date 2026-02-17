using Platform.Core.Events;
using Platform.Integration.Events;
using Xunit;

namespace Platform.Tests;

public class EventBusTests
{
    [Fact]
    public async Task PublishAndSubscribe_ReceivesEvent()
    {
        var bus = new InMemoryEventBus();
        var evt = new SystemEvent { Source = "Test", EventType = "test.event", Department = "Sales" };

        var receiveTask = Task.Run(async () =>
        {
            await foreach (var received in bus.SubscribeAsync())
            {
                return received;
            }
            return null;
        });

        await Task.Delay(50);
        await bus.PublishAsync(evt);

        var result = await receiveTask.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.NotNull(result);
        Assert.Equal("test.event", result!.EventType);
    }

    [Fact]
    public async Task Subscribe_FiltersByDepartment()
    {
        var bus = new InMemoryEventBus();

        var receiveTask = Task.Run(async () =>
        {
            await foreach (var received in bus.SubscribeAsync("Finance"))
            {
                return received;
            }
            return null;
        });

        await Task.Delay(50);
        await bus.PublishAsync(new SystemEvent { Source = "Test", EventType = "sales.event", Department = "Sales" });
        await bus.PublishAsync(new SystemEvent { Source = "Test", EventType = "finance.event", Department = "Finance" });

        var result = await receiveTask.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.NotNull(result);
        Assert.Equal("finance.event", result!.EventType);
    }
}