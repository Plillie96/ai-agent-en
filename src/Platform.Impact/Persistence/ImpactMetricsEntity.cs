namespace Platform.Impact.Persistence;

public sealed class ImpactMetricsEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string WorkflowInstanceId { get; set; } = null!;
    public string Department { get; set; } = null!;
    public DateTimeOffset Timestamp { get; set; }
    public decimal CostSaved { get; set; }
    public decimal RevenueInfluenced { get; set; }
    public long TimeSavedTicks { get; set; }
    public int ManualStepsEliminated { get; set; }
    public double RiskReductionScore { get; set; }
    public string? CustomMetricsJson { get; set; }
}