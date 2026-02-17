using Microsoft.EntityFrameworkCore;
using Platform.Core.Events;
using Platform.Core.Governance;
using Platform.Core.Impact;
using Platform.Core.Integration;
using Platform.Governance.Audit;
using Platform.Governance.Engine;
using Platform.Governance.Persistence;
using Platform.Impact.Persistence;
using Platform.Impact.Tracking;
using Platform.Integration.Connectors;
using Platform.Integration.Events;
using Platform.Intelligence.Reasoning;
using Platform.Orchestration.Engine;
using Platform.Orchestration.Registry;
using Platform.Runtime.Setup;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();

// === PERSISTENT STORAGE (EF Core + SQLite) ===
var dbPath = builder.Configuration.GetValue<string>("Database:Path") ?? "platform.db";
builder.Services.AddDbContext<GovernanceDbContext>(opt => opt.UseSqlite($"Data Source={dbPath}_governance.db"));
builder.Services.AddDbContext<ImpactDbContext>(opt => opt.UseSqlite($"Data Source={dbPath}_impact.db"));

// === AGENT & WORKFLOW REGISTRIES ===
var agentRegistry = PlatformBootstrap.ConfigureAgents();
var workflowRegistry = PlatformBootstrap.ConfigureWorkflows();
builder.Services.AddSingleton(agentRegistry);
builder.Services.AddSingleton(workflowRegistry);

// === GOVERNANCE ===
builder.Services.AddSingleton<IPolicyEngine>(sp =>
    PlatformBootstrap.ConfigurePolicies(sp.GetRequiredService<ILogger<RuleBasedPolicyEngine>>()));

// Storage mode: "InMemory" or "Sqlite" (default)
var storageMode = builder.Configuration.GetValue<string>("Storage:Mode") ?? "Sqlite";

if (storageMode == "InMemory")
{
    var inMemoryAuditLog = new InMemoryAuditLog();
    var inMemoryImpactTracker = new InMemoryImpactTracker();
    builder.Services.AddSingleton<IAuditLog>(inMemoryAuditLog);
    builder.Services.AddSingleton(inMemoryAuditLog);
    builder.Services.AddSingleton<IImpactTracker>(inMemoryImpactTracker);
    builder.Services.AddSingleton(inMemoryImpactTracker);
}
else
{
    builder.Services.AddScoped<IAuditLog, EfCoreAuditLog>();
    builder.Services.AddScoped<IImpactTracker, EfCoreImpactTracker>();
}

// === EVENT BUS ===
var eventBus = new InMemoryEventBus();
builder.Services.AddSingleton<IEventBus>(eventBus);

// === INTELLIGENCE (Azure OpenAI) ===
var aoaiEndpoint = builder.Configuration["AzureOpenAI:Endpoint"];
var aoaiKey = builder.Configuration["AzureOpenAI:ApiKey"];
var aoaiDeployment = builder.Configuration["AzureOpenAI:DeploymentName"];

if (!string.IsNullOrEmpty(aoaiEndpoint) && !string.IsNullOrEmpty(aoaiKey))
{
    var aoaiOptions = new AzureOpenAIReasoningEngineOptions
    {
        Endpoint = aoaiEndpoint,
        ApiKey = aoaiKey,
        DeploymentName = aoaiDeployment ?? "gpt-4o"
    };
    builder.Services.AddSingleton(aoaiOptions);
    builder.Services.AddSingleton<IReasoningEngine, AzureOpenAIReasoningEngine>();
}

// === REAL CONNECTORS (when configured) ===
var sfConfig = builder.Configuration.GetSection("Salesforce");
if (sfConfig.Exists() && !string.IsNullOrEmpty(sfConfig["InstanceUrl"]))
{
    var sfOptions = new SalesforceHttpConnectorOptions
    {
        InstanceUrl = sfConfig["InstanceUrl"]!,
        ClientId = sfConfig["ClientId"]!,
        ClientSecret = sfConfig["ClientSecret"]!,
        Username = sfConfig["Username"]!,
        Password = sfConfig["Password"]!
    };
    builder.Services.AddSingleton(sfOptions);
    builder.Services.AddHttpClient<IEnterpriseConnector, SalesforceHttpConnector>();
}
else
{
    builder.Services.AddSingleton<IEnterpriseConnector, SalesforceConnector>();
}

var snowConfig = builder.Configuration.GetSection("ServiceNow");
if (snowConfig.Exists() && !string.IsNullOrEmpty(snowConfig["InstanceUrl"]))
{
    var snowOptions = new ServiceNowHttpConnectorOptions
    {
        InstanceUrl = snowConfig["InstanceUrl"]!,
        Username = snowConfig["Username"]!,
        Password = snowConfig["Password"]!
    };
    builder.Services.AddSingleton(snowOptions);
    builder.Services.AddHttpClient<ServiceNowHttpConnector>();
    builder.Services.AddSingleton<IEnterpriseConnector>(sp => sp.GetRequiredService<ServiceNowHttpConnector>());
}

// === ORCHESTRATION ===
builder.Services.AddSingleton<WorkflowEngine>();
builder.Services.AddSingleton<EventDrivenOrchestrator>();

// === BACKGROUND SERVICE ===
builder.Services.AddHostedService<OrchestratorBackgroundService>();

var app = builder.Build();

// Ensure databases are created
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<GovernanceDbContext>().Database.EnsureCreated();
    scope.ServiceProvider.GetRequiredService<ImpactDbContext>().Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();

// === HEALTH ===
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTimeOffset.UtcNow }))
    .WithName("Health");

// === AGENTS ===
app.MapGet("/api/agents", (AgentRegistry registry) => registry.ListAll())
    .WithName("ListAgents");

// === WORKFLOWS ===
app.MapGet("/api/workflows", (WorkflowRegistry registry) => registry.ListAll())
    .WithName("ListWorkflows");

app.MapPost("/api/workflows/{workflowId}/execute", async (
    string workflowId,
    Dictionary<string, object>? inputs,
    WorkflowEngine engine,
    WorkflowRegistry workflows,
    IImpactTracker tracker) =>
{
    var workflow = workflows.Resolve(workflowId);
    var instance = await engine.ExecuteAsync(workflow, inputs);

    var totalTimeSaved = TimeSpan.Zero;
    decimal totalRevenue = 0m;
    decimal totalCostSaved = 0m;
    foreach (var step in instance.StepExecutions)
    {
        if (step.Outputs.TryGetValue("TimeSaved", out var ts) && ts is TimeSpan saved)
            totalTimeSaved += saved;
        if (step.Outputs.TryGetValue("RevisedAmount", out var ra) && ra is decimal rev)
            totalRevenue += rev;
        if (step.Outputs.TryGetValue("CostSaved", out var cs) && cs is decimal cost)
            totalCostSaved += cost;
    }

    await tracker.RecordAsync(new ImpactMetrics
    {
        WorkflowInstanceId = instance.InstanceId,
        Department = workflow.Department,
        CostSaved = totalCostSaved + (decimal)totalTimeSaved.TotalHours * 150m,
        RevenueInfluenced = totalRevenue,
        TimeSaved = totalTimeSaved,
        ManualStepsEliminated = instance.StepExecutions.Count(s =>
            s.Status == Platform.Core.Workflows.StepStatus.Completed)
    });

    return Results.Ok(instance);
})
    .WithName("ExecuteWorkflow");

// === EVENTS ===
app.MapPost("/api/events", async (SystemEvent evt, IEventBus bus) =>
{
    await bus.PublishAsync(evt);
    return Results.Accepted(null, new { EventId = evt.EventId, Status = "Published" });
})
    .WithName("PublishEvent");

// === AUDIT ===
app.MapGet("/api/audit", async (IAuditLog log, string? workflowInstanceId, string? agentId, int? limit) =>
{
    var entries = await log.QueryAsync(new AuditQuery
    {
        WorkflowInstanceId = workflowInstanceId,
        AgentId = agentId,
        Limit = limit ?? 100
    });
    return Results.Ok(entries);
})
    .WithName("QueryAudit");

// === IMPACT ===
app.MapGet("/api/impact", async (IImpactTracker tracker, string? department) =>
{
    var summary = await tracker.GetSummaryAsync(department);
    return Results.Ok(summary);
})
    .WithName("GetImpact");

app.MapGet("/api/impact/dashboard", async (IImpactTracker tracker, IAuditLog auditLog) =>
{
    var summary = await tracker.GetSummaryAsync();
    var auditEntries = await auditLog.QueryAsync(new AuditQuery { Limit = 10000 });

    return Results.Ok(new
    {
        Summary = summary,
        AutomationRate = summary.TotalWorkflowsExecuted > 0
            ? (double)summary.TotalStepsAutomated / summary.TotalWorkflowsExecuted
            : 0.0,
        TotalAuditEntries = auditEntries.Count,
        PolicyDenials = auditEntries.Count(e => e.Outcome == AuditOutcome.Denied),
        HumanEscalations = auditEntries.Count(e => e.Outcome == AuditOutcome.EscalatedToHuman),
        Departments = summary.ByDepartment.Keys.ToList(),
        Timestamp = DateTimeOffset.UtcNow
    });
})
    .WithName("ImpactDashboard");

// === GOVERNANCE ===
app.MapPost("/api/governance/evaluate", async (PolicyEvaluationContext context, IPolicyEngine engine) =>
{
    var decision = await engine.EvaluateAsync(context);
    return Results.Ok(decision);
})
    .WithName("EvaluatePolicy");

// === INTELLIGENCE ===
app.MapPost("/api/intelligence/reason", async (
    ReasoningRequest request,
    IReasoningEngine engine) =>
{
    var result = await engine.AnalyzeAsync(request);
    return Results.Ok(result);
})
    .WithName("Reason");

// === CONNECTORS ===
app.MapGet("/api/connectors/health", async (IEnumerable<IEnterpriseConnector> connectors) =>
{
    var results = new List<object>();
    foreach (var connector in connectors)
    {
        var health = await connector.HealthCheckAsync();
        results.Add(new { connector.SystemName, health.IsHealthy, health.Message, Latency = health.Latency.TotalMilliseconds });
    }
    return Results.Ok(results);
})
    .WithName("ConnectorHealth");

app.Run();