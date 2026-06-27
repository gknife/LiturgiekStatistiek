namespace LiturgiekStatistiek.Application.DTOs;

public record ServiceDto(
    Guid Id,
    DateOnly Date,
    string TimeOfDay,
    CongregationSummaryDto Congregation,
    PreacherSummaryDto? Preacher,
    string? ChurchCalendarSunday,
    string? BibleTranslation,
    bool IsReadingService,
    string? ReadSermonBy,
    string? MusicalAccompaniment,
    bool HasBeamerLiturgy,
    bool HasBeamerTexts,
    bool HasBeamerSongs,
    bool HasBeamerTextsAndSongs,
    string? BroadcastUrl,
    string? SpecialOccasion,
    string? SermonText,
    string? SermonTheme,
    string? Notes,
    List<string> Bundles,
    List<ServiceElementDto> Elements,
    DateTime CreatedAt,
    string? CreatedBy,
    int TimeOfDayValue,
    Guid? ChurchCalendarSundayId,
    Guid? BibleTranslationId,
    Guid? MusicalAccompanimentId,
    Guid? SpecialOccasionId,
    List<SermonTextReferenceDto> SermonTextReferences
);

public record SermonTextReferenceDto(
    Guid? BibleBookId,
    string BookName,
    int? Chapter,
    int? VerseStart,
    int? VerseEnd,
    int Position
);

public record SermonTextReferenceRequest(
    Guid? BibleBookId,
    string BookName,
    int? Chapter,
    int? VerseStart,
    int? VerseEnd,
    int Position
);

public record ServiceSummaryDto(
    Guid Id,
    DateOnly Date,
    string TimeOfDay,
    string CongregationName,
    string? City,
    string? PreacherName,
    string? SpecialOccasion,
    int ElementCount,
    string? BroadcastUrl
);

public record CreateServiceRequest(
    DateOnly Date,
    int TimeOfDay,
    Guid CongregationId,
    Guid? PreacherId,
    Guid? ChurchCalendarSundayId,
    Guid? BibleTranslationId,
    bool IsReadingService,
    string? ReadSermonBy,
    Guid? MusicalAccompanimentId,
    bool HasBeamerLiturgy,
    bool HasBeamerTexts,
    bool HasBeamerSongs,
    bool HasBeamerTextsAndSongs,
    string? BroadcastUrl,
    Guid? SpecialOccasionId,
    string? SermonText,
    string? SermonTheme,
    string? Notes,
    List<Guid>? BundleIds,
    List<CreateServiceElementRequest>? Elements,
    List<SermonTextReferenceRequest>? SermonTextReferences
);

public record UpdateServiceRequest(
    DateOnly Date,
    int TimeOfDay,
    Guid CongregationId,
    Guid? PreacherId,
    Guid? ChurchCalendarSundayId,
    Guid? BibleTranslationId,
    bool IsReadingService,
    string? ReadSermonBy,
    Guid? MusicalAccompanimentId,
    bool HasBeamerLiturgy,
    bool HasBeamerTexts,
    bool HasBeamerSongs,
    bool HasBeamerTextsAndSongs,
    string? BroadcastUrl,
    Guid? SpecialOccasionId,
    string? SermonText,
    string? SermonTheme,
    string? Notes,
    List<Guid>? BundleIds,
    List<CreateServiceElementRequest>? Elements,
    List<SermonTextReferenceRequest>? SermonTextReferences
);

/// <summary>
/// Applies a single flat-field change to many services at once. <see cref="Field"/>
/// is whitelisted server-side.
/// </summary>
public record BulkUpdateServicesRequest(
    List<Guid> ServiceIds,
    string Field,
    string? Value
);

public record BulkDeleteServicesRequest(
    List<Guid> ServiceIds
);

public record BulkOperationResult(
    int Affected
);
