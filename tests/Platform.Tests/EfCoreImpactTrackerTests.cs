using Microsoft.EntityFrameworkCore;
using Platform.Core.Impact;
using Platform.Impact.Persistence;
using Xunit;

namespace Platform.Tests;

public class EfCoreImpactTrackerTests : IDisposable
{
    private readonly ImpactDbContext _db;
    private readonly EfCoreImpactTracker _tracker;

    public EfCoreImpactTrackerTests()
    {
        var options = new DbContextOptionsBuilder<ImpactDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        _db = new ImpactDbContext(options);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();
        _tracker = new EfCoreImpactTracker(_db);
    }

    [Fact]
    public async Task RecordAndSummarize_PersistsCorrectly()
    {
        await _tracker.RecordAsync(new ImpactMetrics
        {
            WorkflowInstanceId = "wf-1",
            Department = "Sales",
            CostSaved = 5000m,
            RevenueInfluenced = 480000m,
            TimeSaved = TimeSpan.FromHours(2),
            ManualStepsEliminated = 4
        });

        await _tracker.RecordAsync(new ImpactMetrics
        {
            WorkflowInstanceId = "wf-2",
            Department = "Finance",
            CostSaved = 12000m,
            TimeSaved = TimeSpan.FromHours(8),
            ManualStepsEliminated = 2
        });

        var summary = await _tracker.GetSummaryAsync();

        Assert.Equal(17000m, summary.TotalCostSaved);
        Assert.Equal(2, summary.TotalWorkflowsExecuted);
        Assert.Equal(2, summary.ByDepartment.Count);
    }

    [Fact]
    public async Task GetSummaryAsync_FiltersByDepartment()
    {
        await _tracker.RecordAsync(new ImpactMetrics { WorkflowInstanceId = "wf-1", Department = "Sales", CostSaved = 5000m });
        await _tracker.RecordAsync(new ImpactMetrics { WorkflowInstanceId = "wf-2", Department = "IT", CostSaved = 3000m });

        var summary = await _tracker.GetSummaryAsync("Sales");

        Assert.Equal(5000m, summary.TotalCostSaved);
        Assert.Single(summary.ByDepartment);
    }

    public void Dispose() => _db.Dispose();
}