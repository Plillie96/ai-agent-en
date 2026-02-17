using Platform.Core.Impact;
using Platform.Impact.Tracking;
using Xunit;

namespace Platform.Tests;

public class ImpactTrackerTests
{
    [Fact]
    public async Task GetSummaryAsync_AggregatesCorrectly()
    {
        var tracker = new InMemoryImpactTracker();

        await tracker.RecordAsync(new ImpactMetrics
        {
            WorkflowInstanceId = "wf-1",
            Department = "Sales",
            CostSaved = 5000m,
            RevenueInfluenced = 480000m,
            TimeSaved = TimeSpan.FromHours(2),
            ManualStepsEliminated = 4
        });

        await tracker.RecordAsync(new ImpactMetrics
        {
            WorkflowInstanceId = "wf-2",
            Department = "Sales",
            CostSaved = 3000m,
            RevenueInfluenced = 120000m,
            TimeSaved = TimeSpan.FromHours(1),
            ManualStepsEliminated = 3
        });

        await tracker.RecordAsync(new ImpactMetrics
        {
            WorkflowInstanceId = "wf-3",
            Department = "Finance",
            CostSaved = 8000m,
            RevenueInfluenced = 0m,
            TimeSaved = TimeSpan.FromHours(4),
            ManualStepsEliminated = 6
        });

        var summary = await tracker.GetSummaryAsync();

        Assert.Equal(16000m, summary.TotalCostSaved);
        Assert.Equal(600000m, summary.TotalRevenueInfluenced);
        Assert.Equal(3, summary.TotalWorkflowsExecuted);
        Assert.Equal(13, summary.TotalStepsAutomated);
        Assert.Equal(2, summary.ByDepartment.Count);
    }

    [Fact]
    public async Task GetSummaryAsync_FiltersByDepartment()
    {
        var tracker = new InMemoryImpactTracker();

        await tracker.RecordAsync(new ImpactMetrics
        {
            WorkflowInstanceId = "wf-1",
            Department = "Sales",
            CostSaved = 5000m,
            ManualStepsEliminated = 4
        });

        await tracker.RecordAsync(new ImpactMetrics
        {
            WorkflowInstanceId = "wf-2",
            Department = "Finance",
            CostSaved = 8000m,
            ManualStepsEliminated = 6
        });

        var salesSummary = await tracker.GetSummaryAsync("Sales");

        Assert.Equal(5000m, salesSummary.TotalCostSaved);
        Assert.Equal(1, salesSummary.TotalWorkflowsExecuted);
    }
}