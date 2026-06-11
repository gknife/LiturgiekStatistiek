namespace LiturgiekStatistiek.Application.DTOs;

public record SongDto(
    Guid Id,
    Guid BundleId,
    string BundleName,
    string? BundleAbbreviation,
    int Number,
    string? Title,
    int? NumberOfVerses
);

public record CreateSongRequest(
    Guid BundleId,
    int Number,
    string? Title,
    int? NumberOfVerses
);

public record UpdateSongRequest(
    string? Title,
    int? NumberOfVerses
);
