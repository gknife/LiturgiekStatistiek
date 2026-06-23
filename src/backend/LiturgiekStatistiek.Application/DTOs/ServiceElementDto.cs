namespace LiturgiekStatistiek.Application.DTOs;

public record ServiceElementDto(
    Guid Id,
    int Position,
    string ElementType,
    string? Label,
    string? ScriptureReference,
    string? Notes,
    List<ServiceElementSongDto> Songs
);

public record CreateServiceElementRequest(
    int Position,
    int ElementType,
    Guid? LabelId,
    string? ScriptureReference,
    string? Notes,
    List<CreateServiceElementSongRequest>? Songs
);

public record ServiceElementSongDto(
    Guid Id,
    string BundleName,
    string? BundleAbbreviation,
    string Section,
    int SongNumber,
    int Position,
    List<string> Verses
);

public record CreateServiceElementSongRequest(
    Guid BundleId,
    string? Section,
    int SongNumber,
    int Position,
    List<string>? Verses
);
