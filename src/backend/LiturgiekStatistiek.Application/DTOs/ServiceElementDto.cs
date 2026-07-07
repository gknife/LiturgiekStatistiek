namespace LiturgiekStatistiek.Application.DTOs;

public record ServiceElementDto(
    Guid Id,
    int Position,
    string ElementType,
    string? Label,
    string? ScriptureReference,
    string? Notes,
    List<ServiceElementSongDto> Songs,
    Guid? LabelId = null,
    Guid? PerformerId = null,
    string? Performer = null,
    bool IsBeurtzang = false,
    Guid? BibleTranslationId = null,
    string? BibleTranslation = null,
    List<ReadingReferenceDto>? ReadingReferences = null
);

public record CreateServiceElementRequest(
    int Position,
    int ElementType,
    Guid? LabelId,
    string? ScriptureReference,
    string? Notes,
    List<CreateServiceElementSongRequest>? Songs,
    Guid? PerformerId = null,
    bool IsBeurtzang = false,
    Guid? BibleTranslationId = null,
    List<ReadingReferenceRequest>? ReadingReferences = null
);

public record ServiceElementSongDto(
    Guid Id,
    string BundleName,
    string? BundleAbbreviation,
    string Section,
    int SongNumber,
    int Position,
    List<string> Verses,
    Guid? BundleId = null,
    SongCompletenessDto? Completeness = null
);

public record CreateServiceElementSongRequest(
    Guid BundleId,
    string? Section,
    int SongNumber,
    int Position,
    List<string>? Verses
);

public record ReadingReferenceDto(
    Guid? BibleBookId,
    string BookName,
    int? Chapter,
    int? VerseStart,
    int? VerseEnd,
    int Position
);

public record ReadingReferenceRequest(
    Guid? BibleBookId,
    string BookName,
    int? Chapter,
    int? VerseStart,
    int? VerseEnd,
    int Position
);

/// <summary>
/// Computed completeness of a sung song against its catalog verse count.
/// <see cref="State"/> is one of: "compleet-onderdeel" (all verses in this single
/// onderdeel), "compleet-dienst" (all verses across the whole service),
/// "onvolledig", or "onbekend" (no catalog verse-count available).
/// </summary>
public record SongCompletenessDto(
    string State,
    bool CompleteInElement,
    bool CompleteInService,
    int? CatalogVerseCount,
    int SungVerseCount
);

