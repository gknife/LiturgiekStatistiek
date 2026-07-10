namespace LiturgiekStatistiek.Application.DTOs;

public record ServiceTemplateDto(
    Guid Id,
    string Name,
    Guid? DenominationId,
    string? Denomination,
    Guid? CongregationId,
    string? Congregation,
    int? TimeOfDay,
    Guid? OccasionId,
    string? Occasion,
    bool IsActive,
    List<ServiceTemplateElementDto> Elements,
    Guid? MusicalAccompanimentId = null,
    string? MusicalAccompaniment = null,
    bool IsReadingService = false,
    bool HasBeamerLiturgy = false,
    bool HasBeamerTexts = false,
    bool HasBeamerSongs = false,
    Guid? DefaultBibleTranslationId = null,
    string? DefaultBibleTranslation = null
);

public record ServiceTemplateElementDto(
    Guid Id,
    int Position,
    string ElementType,
    int ElementTypeValue,
    Guid? LabelId,
    string? Label,
    Guid? PerformerId,
    string? Performer,
    bool IsBeurtzang,
    string? FixedScriptureReference
);

public record ServiceTemplateSummaryDto(
    Guid Id,
    string Name,
    Guid? DenominationId,
    string? Denomination,
    Guid? CongregationId,
    string? Congregation,
    int? TimeOfDay,
    Guid? OccasionId,
    string? Occasion,
    bool IsActive,
    int ElementCount
);

public record CreateServiceTemplateRequest(
    string Name,
    Guid? DenominationId,
    Guid? CongregationId,
    int? TimeOfDay,
    Guid? OccasionId,
    bool IsActive,
    List<CreateServiceTemplateElementRequest> Elements,
    Guid? MusicalAccompanimentId = null,
    bool IsReadingService = false,
    bool HasBeamerLiturgy = false,
    bool HasBeamerTexts = false,
    bool HasBeamerSongs = false,
    Guid? DefaultBibleTranslationId = null
);

public record CreateServiceTemplateElementRequest(
    int Position,
    int ElementType,
    Guid? LabelId,
    Guid? PerformerId,
    bool IsBeurtzang,
    string? FixedScriptureReference
);
