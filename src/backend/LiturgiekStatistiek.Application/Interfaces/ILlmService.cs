namespace LiturgiekStatistiek.Application.Interfaces;

public interface ILlmService
{
    Task<LlmQueryParseResult> ParseNaturalLanguageQueryAsync(string query, CancellationToken ct = default);
    Task<LlmLiturgyParseResult> ParseLiturgyTextAsync(string text, CancellationToken ct = default);
}

public record LlmQueryParseResult
{
    public bool Success { get; init; }
    public string? TemplateId { get; init; }
    public Dictionary<string, string>? Parameters { get; init; }
    public string? ErrorMessage { get; init; }
}

public record LlmLiturgyParseResult
{
    public bool Success { get; init; }
    public ParsedServiceData? Data { get; init; }
    public string? ErrorMessage { get; init; }
}

public record ParsedServiceData
{
    public string? City { get; init; }
    public string? Congregation { get; init; }
    public string? Denomination { get; init; }
    public string? Date { get; init; }
    public string? TimeOfDay { get; init; }
    public string? Preacher { get; init; }
    public string? BibleTranslation { get; init; }
    public string? SermonText { get; init; }
    public string? SermonTheme { get; init; }
    public string? BroadcastUrl { get; init; }
    public List<ParsedElement> Elements { get; init; } = new();
}

public record ParsedElement
{
    public int Position { get; init; }
    public string Label { get; init; } = string.Empty;
    public string? SongBundle { get; init; }
    public int? SongNumber { get; init; }
    public List<string>? Verses { get; init; }
    public string? Notes { get; init; }
}
