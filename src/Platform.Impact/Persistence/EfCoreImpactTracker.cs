using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Platform.Core.Impact;

namespace Platform.Impact.Persistence;

public sealed class EfCoreImpactTracker : IImpactTracker
{
    private readonly ImpactDbContext _db;

    public EfCoreImpactTracker(ImpactDbContext db)
    {
        _db = db;
    }

    public async Task RecordAsync(ImpactMetrics metrics, CancellationToken ct = default)
    {
        _db.ImpactMetrics.Add(new ImpactMetricsEntity
        {
            WorkflowInstanceId = metrics.WorkflowInstanceId,
            Department = metrics.Department,
            Timestamp = metrics.Timestamp,
            CostSaved = metrics.CostSaved,
            RevenueInfluenced = metrics.RevenueInfluenced,
            TimeSavedTicks = metrics.TimeSaved.Ticks,
            ManualStepsEliminated = metrics.ManualStepsEliminated,
            RiskReductionScore = metrics.RiskReductionScore,
            CustomMetricsJson = metrics.CustomMetrics.Count > 0
                ? JsonSerializer.Serialize(metrics.CustomMetrics)
                : null
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task<ImpactSummary> GetSummaryAsync(string? department = null, DateTimeOffset? since = null, CancellationToken ct = default)
    {
        var query = _db.ImpactMetrics.AsQueryable();

        if (department is not null)
            query = query.Where(m => m.Department == department);
        if (since.HasValue)
            query = query.Where(m => m.Timestamp >= since.Value);

        var entities = await query.ToListAsync(ct);

        var byDept = entities
            .GroupBy(m => m.Department, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => new DepartmentImpact
                {
                    Department = g.Key,
                    CostSaved = g.Sum(m => m.CostSaved),
                    RevenueInfluenced = g.Sum(m => m.RevenueInfluenced),
                    TimeSaved = TimeSpan.FromTicks(g.Sum(m => m.TimeSavedTicks)),
                    WorkflowsExecuted = g.Count()
                });

        return new ImpactSummary
        {
            TotalCostSaved = entities.Sum(m => m.CostSaved),
            TotalRevenueInfluenced = entities.Sum(m => m.RevenueInfluenced),
            TotalTimeSaved = TimeSpan.FromTicks(entities.Sum(m => m.TimeSavedTicks)),
            TotalWorkflowsExecuted = entities.Count,
            TotalStepsAutomated = entities.Sum(m => m.ManualStepsEliminated),
            ByDepartment = byDept
        };
    }
}