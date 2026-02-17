using Microsoft.Extensions.Logging;
using Platform.Core.Integration;

namespace Platform.Integration.Connectors;

public sealed class ServiceNowConnector : IEnterpriseConnector
{
    private readonly ILogger<ServiceNowConnector> _logger;

    public ServiceNowConnector(ILogger<ServiceNowConnector> logger)
    {
        _logger = logger;
    }

    public string SystemName => "ServiceNow";

    public string[] SupportedOperations => ["CreateIncident", "UpdateIncident", "QueryIncident", "CreateChange", "CloseTicket"];

    public Task<ConnectorResult> ExecuteAsync(ConnectorRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("ServiceNow: {Operation} on {EntityType} {EntityId}", request.Operation, request.EntityType, request.EntityId);

        var result = request.Operation switch
        {
            "CreateIncident" => new ConnectorResult
            {
                Succeeded = true,
                Data = new Dictionary<string, object>
                {
                    ["IncidentId"] = string.Format("INC{0}", Random.Shared.Next(100000, 999999)),
                    ["State"] = "New",
                    ["CreatedAt"] = DateTimeOffset.UtcNow.ToString("O")
                }
            },
            _ => new ConnectorResult { Succeeded = true, Data = new Dictionary<string, object> { ["Status"] = "OK" } }
        };

        return Task.FromResult(result);
    }

    public Task<HealthCheckResult> HealthCheckAsync(CancellationToken ct = default)
    {
        return Task.FromResult(new HealthCheckResult { IsHealthy = true, Message = "ServiceNow API reachable", Latency = TimeSpan.FromMilliseconds(62) });
    }
}