namespace Platform.Governance.Persistence;

public sealed class AuditEntryEntity
{
    public string EntryId { get; set; } = null!;
    public string WorkflowInstanceId { get; set; } = null!;
    public string StepId { get; set; } = null!;
    public string AgentId { get; set; } = null!;
    public string Action { get; set; } = null!;
    public int Outcome { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string? PolicyId { get; set; }
    public string? Justification { get; set; }
    public string? DetailsJson { get; set; }
    public string? ApprovedBy { get; set; }
}