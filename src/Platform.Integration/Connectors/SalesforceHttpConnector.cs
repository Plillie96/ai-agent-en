using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Platform.Core.Integration;

namespace Platform.Integration.Connectors;

public sealed class SalesforceHttpConnectorOptions
{
    public required string InstanceUrl { get; init; }
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
    public required string Username { get; init; }
    public required string Password { get; init; }
}

public sealed class SalesforceHttpConnector : IEnterpriseConnector
{
    private readonly HttpClient _httpClient;
    private readonly SalesforceHttpConnectorOptions _options;
    private readonly ILogger<SalesforceHttpConnector> _logger;
    private string? _accessToken;
    private DateTimeOffset _tokenExpiry;

    public SalesforceHttpConnector(HttpClient httpClient, SalesforceHttpConnectorOptions options, ILogger<SalesforceHttpConnector> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    public string SystemName => "Salesforce";
    public string[] SupportedOperations => ["Query", "Create", "Update", "Delete"];

    public async Task<ConnectorResult> ExecuteAsync(ConnectorRequest request, CancellationToken ct = default)
    {
        try
        {
            await EnsureAuthenticatedAsync(ct);

            return request.Operation switch
            {
                "Query" => await QueryAsync(request, ct),
                "Create" => await CreateAsync(request, ct),
                "Update" => await UpdateAsync(request, ct),
                "Delete" => await DeleteAsync(request, ct),
                _ => new ConnectorResult { Succeeded = false, ErrorMessage = $"Unsupported operation: {request.Operation}" }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Salesforce {Operation} failed for {EntityType}", request.Operation, request.EntityType);
            return new ConnectorResult { Succeeded = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<HealthCheckResult> HealthCheckAsync(CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await EnsureAuthenticatedAsync(ct);
            var response = await _httpClient.GetAsync($"{_options.InstanceUrl}/services/data/v59.0/limits", ct);
            sw.Stop();
            return new HealthCheckResult { IsHealthy = response.IsSuccessStatusCode, Latency = sw.Elapsed, Message = $"HTTP {(int)response.StatusCode}" };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new HealthCheckResult { IsHealthy = false, Latency = sw.Elapsed, Message = ex.Message };
        }
    }

    private async Task EnsureAuthenticatedAsync(CancellationToken ct)
    {
        if (_accessToken is not null && DateTimeOffset.UtcNow < _tokenExpiry)
            return;

        _logger.LogInformation("Authenticating with Salesforce OAuth2");

        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["username"] = _options.Username,
            ["password"] = _options.Password
        });

        var response = await _httpClient.PostAsync($"{_options.InstanceUrl}/services/oauth2/token", tokenRequest, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        _accessToken = json.GetProperty("access_token").GetString();
        _tokenExpiry = DateTimeOffset.UtcNow.AddHours(1);

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
    }

    private async Task<ConnectorResult> QueryAsync(ConnectorRequest request, CancellationToken ct)
    {
        var soql = request.Parameters.TryGetValue("query", out var q) ? q.ToString() : $"SELECT Id, Name FROM {request.EntityType} LIMIT 10";
        var encoded = Uri.EscapeDataString(soql!);
        var response = await _httpClient.GetAsync($"{_options.InstanceUrl}/services/data/v59.0/query?q={encoded}", ct);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        return new ConnectorResult
        {
            Succeeded = response.IsSuccessStatusCode,
            HttpStatusCode = (int)response.StatusCode,
            Data = new Dictionary<string, object> { ["result"] = json.ToString()! }
        };
    }

    private async Task<ConnectorResult> CreateAsync(ConnectorRequest request, CancellationToken ct)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"{_options.InstanceUrl}/services/data/v59.0/sobjects/{request.EntityType}",
            request.Parameters, ct);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        return new ConnectorResult
        {
            Succeeded = response.IsSuccessStatusCode,
            HttpStatusCode = (int)response.StatusCode,
            Data = new Dictionary<string, object> { ["result"] = json.ToString()! }
        };
    }

    private async Task<ConnectorResult> UpdateAsync(ConnectorRequest request, CancellationToken ct)
    {
        var content = JsonContent.Create(request.Parameters);
        var httpRequest = new HttpRequestMessage(HttpMethod.Patch,
            $"{_options.InstanceUrl}/services/data/v59.0/sobjects/{request.EntityType}/{request.EntityId}")
        { Content = content };

        var response = await _httpClient.SendAsync(httpRequest, ct);
        return new ConnectorResult
        {
            Succeeded = response.IsSuccessStatusCode,
            HttpStatusCode = (int)response.StatusCode
        };
    }

    private async Task<ConnectorResult> DeleteAsync(ConnectorRequest request, CancellationToken ct)
    {
        var response = await _httpClient.DeleteAsync(
            $"{_options.InstanceUrl}/services/data/v59.0/sobjects/{request.EntityType}/{request.EntityId}", ct);
        return new ConnectorResult
        {
            Succeeded = response.IsSuccessStatusCode,
            HttpStatusCode = (int)response.StatusCode
        };
    }
}