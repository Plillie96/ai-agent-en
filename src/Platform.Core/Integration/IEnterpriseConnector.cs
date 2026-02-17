namespace Platform.Core.Integration;

public interface IEnterpriseConnector
{
    string SystemName { get; }
    string[] SupportedOperations { get; }
    Task<ConnectorResult> ExecuteAsync(ConnectorRequest request, CancellationToken ct = default);
    Task<HealthCheckResult> HealthCheckAsync(CancellationToken ct = default);
}

public sealed class ConnectorRequest
{
    public required string Operation { get; init; }
    public required string EntityType { get; init; }
    public string? EntityId { get; init; }
    public Dictionary<string, object> Parameters { get; init; } = [];
}

public sealed class ConnectorResult
{
    public required bool Succeeded { get; init; }
    public Dictionary<string, object> Data { get; init; } = [];
    public string? ErrorMessage { get; init; }
    public int? HttpStatusCode { get; init; }
}

public sealed class HealthCheckResult
{
    public required bool IsHealthy { get; init; }
    public string? Message { get; init; }
    public TimeSpan Latency { get; init; }
}
