using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Platform.Core.Integration;

namespace Platform.Integration.Connectors;

public sealed class ServiceNowHttpConnectorOptions
{
    public required string InstanceUrl { get; init; }
    public required string Username { get; init; }
    public required string Password { get; init; }
}

public sealed class ServiceNowHttpConnector : IEnterpriseConnector
{
    private readonly HttpClient _httpClient;
    private readonly ServiceNowHttpConnectorOptions _options;
    private readonly ILogger<ServiceNowHttpConnector> _logger;

    public ServiceNowHttpConnector(HttpClient httpClient, ServiceNowHttpConnectorOptions options, ILogger<ServiceNowHttpConnector> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.Username}:{options.Password}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public string SystemName => "ServiceNow";
    public string[] SupportedOperations => ["CreateIncident", "UpdateIncident", "QueryIncident", "CreateChange", "CloseTicket"];

    public async Task<ConnectorResult> ExecuteAsync(ConnectorRequest request, CancellationToken ct = default)
    {
        try
        {
            return request.Operation switch
            {
                "CreateIncident" => await CreateRecordAsync("incident", request.Parameters, ct),
                "UpdateIncident" => await UpdateRecordAsync("incident", request.EntityId!, request.Parameters, ct),
                "QueryIncident" => await QueryRecordsAsync("incident", request.Parameters, ct),
                "CreateChange" => await CreateRecordAsync("change_request", request.Parameters, ct),
                "CloseTicket" => await UpdateRecordAsync("incident", request.EntityId!,
                    new Dictionary<string, object> { ["state"] = "7", ["close_code"] = "Solved", ["close_notes"] = "Resolved by automation" }, ct),
                _ => new ConnectorResult { Succeeded = false, ErrorMessage = $"Unsupported operation: {request.Operation}" }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ServiceNow {Operation} failed", request.Operation);
            return new ConnectorResult { Succeeded = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<HealthCheckResult> HealthCheckAsync(CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var response = await _httpClient.GetAsync($"{_options.InstanceUrl}/api/now/table/sys_properties?sysparm_limit=1", ct);
            sw.Stop();
            return new HealthCheckResult { IsHealthy = response.IsSuccessStatusCode, Latency = sw.Elapsed, Message = $"HTTP {(int)response.StatusCode}" };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new HealthCheckResult { IsHealthy = false, Latency = sw.Elapsed, Message = ex.Message };
        }
    }

    private async Task<ConnectorResult> CreateRecordAsync(string table, Dictionary<string, object> fields, CancellationToken ct)
    {
        var response = await _httpClient.PostAsJsonAsync($"{_options.InstanceUrl}/api/now/table/{table}", fields, ct);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        var data = new Dictionary<string, object> { ["result"] = json.ToString()! };
        if (json.TryGetProperty("result", out var result) && result.TryGetProperty("sys_id", out var sysId))
            data["sys_id"] = sysId.GetString()!;

        return new ConnectorResult { Succeeded = response.IsSuccessStatusCode, HttpStatusCode = (int)response.StatusCode, Data = data };
    }

    private async Task<ConnectorResult> UpdateRecordAsync(string table, string sysId, Dictionary<string, object> fields, CancellationToken ct)
    {
        var content = JsonContent.Create(fields);
        var request = new HttpRequestMessage(HttpMethod.Patch, $"{_options.InstanceUrl}/api/now/table/{table}/{sysId}") { Content = content };
        var response = await _httpClient.SendAsync(request, ct);

        return new ConnectorResult { Succeeded = response.IsSuccessStatusCode, HttpStatusCode = (int)response.StatusCode };
    }

    private async Task<ConnectorResult> QueryRecordsAsync(string table, Dictionary<string, object> parameters, CancellationToken ct)
    {
        var query = parameters.TryGetValue("query", out var q) ? q.ToString() : "";
        var limit = parameters.TryGetValue("limit", out var l) ? l.ToString() : "10";
        var response = await _httpClient.GetAsync($"{_options.InstanceUrl}/api/now/table/{table}?sysparm_query={Uri.EscapeDataString(query!)}&sysparm_limit={limit}", ct);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        return new ConnectorResult
        {
            Succeeded = response.IsSuccessStatusCode,
            HttpStatusCode = (int)response.StatusCode,
            Data = new Dictionary<string, object> { ["result"] = json.ToString()! }
        };
    }
}