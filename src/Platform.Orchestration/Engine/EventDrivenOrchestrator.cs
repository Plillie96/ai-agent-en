using Microsoft.Extensions.Logging;
using Platform.Core.Events;
using Platform.Orchestration.Registry;

namespace Platform.Orchestration.Engine;

public sealed class EventDrivenOrchestrator
{
    private readonly IEventBus _eventBus;
    private readonly WorkflowRegistry _workflows;
    private readonly WorkflowEngine _engine;
    private readonly ILogger<EventDrivenOrchestrator> _logger;

    public EventDrivenOrchestrator(IEventBus eventBus, WorkflowRegistry workflows, WorkflowEngine engine, ILogger<EventDrivenOrchestrator> logger)
    {
        _eventBus = eventBus;
        _workflows = workflows;
        _engine = engine;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Event-driven orchestrator started. Listening for system events...");

        await foreach (var evt in _eventBus.SubscribeAsync(ct: ct))
        {
            _logger.LogInformation("Received event {EventType} from {Source}", evt.EventType, evt.Source);

            var workflow = _workflows.FindByTrigger(evt.EventType);
            if (workflow is null)
            {
                _logger.LogDebug("No workflow registered for event type {EventType}", evt.EventType);
                continue;
            }

            _logger.LogInformation("Triggering workflow {WorkflowId} for event {EventType}", workflow.WorkflowId, evt.EventType);

            var inputs = new Dictionary<string, object>(evt.Payload)
            {
                ["_eventId"] = evt.EventId,
                ["_eventSource"] = evt.Source,
                ["_eventTimestamp"] = evt.Timestamp
            };

            _ = Task.Run(async () =>
            {
                try
                {
                    await _engine.ExecuteAsync(workflow, inputs, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to execute workflow {WorkflowId} for event {EventId}", workflow.WorkflowId, evt.EventId);
                }
            }, ct);
        }
    }
}