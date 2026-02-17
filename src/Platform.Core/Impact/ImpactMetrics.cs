namespace Platform.Core.Impact;

public sealed class ImpactMetrics
{
    public required string WorkflowInstanceId { get; init; }
    public required string Department { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public decimal CostSaved { get; init; }
    public decimal RevenueInfluenced { get; init; }
    public TimeSpan TimeSaved { get; init; }
    public int ManualStepsEliminated { get; init; }
    public double RiskReductionScore { get; init; }
    public Dictionary<string, decimal> CustomMetrics { get; init; } = [];
}

public interface IImpactTracker
{
    Task RecordAsync(ImpactMetrics metrics, CancellationToken ct = default);
    Task<ImpactSummary> GetSummaryAsync(string? department = null, DateTimeOffset? since = null, CancellationToken ct = default);
}

public sealed class ImpactSummary
{
    public decimal TotalCostSaved { get; init; }
    public decimal TotalRevenueInfluenced { get; init; }
    public TimeSpan TotalTimeSaved { get; init; }
    public int TotalWorkflowsExecuted { get; init; }
    public int TotalStepsAutomated { get; init; }
    public Dictionary<string, DepartmentImpact> ByDepartment { get; init; } = [];
}

public sealed class DepartmentImpact
{
    public required string Department { get; init; }
    public decimal CostSaved { get; init; }
    public decimal RevenueInfluenced { get; init; }
    public TimeSpan TimeSaved { get; init; }
    public int WorkflowsExecuted { get; init; }
}
