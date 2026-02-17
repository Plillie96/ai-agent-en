namespace Platform.Intelligence.Reasoning;

public interface IReasoningEngine
{
    Task<ReasoningResult> AnalyzeAsync(ReasoningRequest request, CancellationToken ct = default);
}

public sealed class ReasoningRequest
{
    public required string Prompt { get; init; }
    public required string Context { get; init; }
    public Dictionary<string, object> Parameters { get; init; } = [];
    public string? SystemInstruction { get; init; }
    public double Temperature { get; init; } = 0.3;
    public int MaxTokens { get; init; } = 2000;
}

public sealed class ReasoningResult
{
    public required bool Succeeded { get; init; }
    public required string Response { get; init; }
    public double Confidence { get; init; }
    public string? Reasoning { get; init; }
    public Dictionary<string, object> ExtractedData { get; init; } = [];
    public int TokensUsed { get; init; }
}