using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Platform.Orchestration.Engine;

public sealed class OrchestratorBackgroundService : BackgroundService
{
    private readonly EventDrivenOrchestrator _orchestrator;
    private readonly ILogger<OrchestratorBackgroundService> _logger;

    public OrchestratorBackgroundService(EventDrivenOrchestrator orchestrator, ILogger<OrchestratorBackgroundService> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Orchestrator background service starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _orchestrator.RunAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Orchestrator background service stopping");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Orchestrator background service encountered an error, restarting in 5s");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}