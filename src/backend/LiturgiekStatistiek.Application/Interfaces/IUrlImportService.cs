namespace LiturgiekStatistiek.Application.Interfaces;

/// <summary>
/// Fetches a broadcast page server-side and extracts a liturgy from its
/// <c>og:title</c>/<c>&lt;title&gt;</c> and <c>meta description</c>, then parses it
/// into structured <see cref="ParsedServiceData"/> for review in the add form.
/// </summary>
public interface IUrlImportService
{
    Task<UrlImportResult> ImportAsync(string url, CancellationToken ct = default);
}

public record UrlImportResult
{
    public bool Success { get; init; }
    public ParsedServiceData? Data { get; init; }
    public string? ErrorMessage { get; init; }
    public string? RawDescription { get; init; }
}
