namespace LiturgiekStatistiek.Application.DTOs;

/// <summary>
/// A Bible book with the display name resolved for a requested translation and the
/// versification table (verse count per chapter) for chapter/verse dropdowns.
/// </summary>
public record BibleBookDto(
    Guid Id,
    int Ordinal,
    string Testament,
    string Name,
    int ChapterCount,
    IReadOnlyList<int> VerseCounts);
