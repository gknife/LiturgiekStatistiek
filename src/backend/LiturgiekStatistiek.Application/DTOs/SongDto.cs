namespace LiturgiekStatistiek.Application.DTOs;

public record SongVerseDto(
    int Number,
    string? Title,
    string? Label = null,
    int SortOrder = 0
);

public record SongDto(
    Guid Id,
    Guid BundleId,
    string BundleName,
    string? BundleAbbreviation,
    string Section,
    int Number,
    string? Title,
    int? NumberOfVerses,
    IReadOnlyList<SongVerseDto>? Verses = null
);

public record CreateSongRequest(
    Guid BundleId,
    string? Section,
    int Number,
    string? Title,
    int? NumberOfVerses,
    IReadOnlyList<SongVerseDto>? Verses = null
);

public record UpdateSongRequest(
    string? Section,
    int? Number,
    string? Title,
    int? NumberOfVerses,
    IReadOnlyList<SongVerseDto>? Verses = null
);
