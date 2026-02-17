using System.Collections.Concurrent;
using Platform.Core.Impact;

namespace Platform.Impact.Tracking;

public sealed class InMemoryImpactTracker : IImpactTracker
{
    private readonly ConcurrentBag<ImpactMetrics> _metrics = [];

    public Task RecordAsync(ImpactMetrics metrics, CancellationToken ct = default)
    {
        _metrics.Add(metrics);
        return Task.CompletedTask;
    }

    public Task<ImpactSummary> GetSummaryAsync(string? department = null, DateTimeOffset? since = null, CancellationToken ct = default)
    {
        var filtered = _metrics.AsEnumerable();

        if (department is not null)
            filtered = filtered.Where(m => string.Equals(m.Department, department, StringComparison.OrdinalIgnoreCase));
        if (since.HasValue)
            filtered = filtered.Where(m => m.Timestamp >= since.Value);

        var list = filtered.ToList();

        var byDept = list
            .GroupBy(m => m.Department, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => new DepartmentImpact
                {
                    Department = g.Key,
                    CostSaved = g.Sum(m => m.CostSaved),
                    RevenueInfluenced = g.Sum(m => m.RevenueInfluenced),
                    TimeSaved = TimeSpan.FromTicks(g.Sum(m => m.TimeSaved.Ticks)),
                    WorkflowsExecuted = g.Count()
                });

        var summary = new ImpactSummary
        {
            TotalCostSaved = list.Sum(m => m.CostSaved),
            TotalRevenueInfluenced = list.Sum(m => m.RevenueInfluenced),
            TotalTimeSaved = TimeSpan.FromTicks(list.Sum(m => m.TimeSaved.Ticks)),
            TotalWorkflowsExecuted = list.Count,
            TotalStepsAutomated = list.Sum(m => m.ManualStepsEliminated),
            ByDepartment = byDept
        };

        return Task.FromResult(summary);
    }

    public IReadOnlyCollection<ImpactMetrics> GetAll() => [.. _metrics];
}