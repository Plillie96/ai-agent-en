using Microsoft.Extensions.DependencyInjection;
using Platform.Core.Impact;

namespace Platform.Impact.Persistence;

public sealed class ScopedImpactTracker : IImpactTracker
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ScopedImpactTracker(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task RecordAsync(ImpactMetrics metrics, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var inner = scope.ServiceProvider.GetRequiredService<EfCoreImpactTracker>();
        await inner.RecordAsync(metrics, ct);
    }

    public async Task<ImpactSummary> GetSummaryAsync(string? department = null, DateTimeOffset? since = null, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var inner = scope.ServiceProvider.GetRequiredService<EfCoreImpactTracker>();
        return await inner.GetSummaryAsync(department, since, ct);
    }
}