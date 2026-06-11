namespace LiturgiekStatistiek.Application.DTOs;

public record CongregationDto(
    Guid Id,
    string Name,
    string City,
    string? LocationDetail,
    string? Denomination,
    string? DenominationAbbreviation,
    string? Modality,
    decimal? Latitude,
    decimal? Longitude
);

public record CongregationSummaryDto(
    Guid Id,
    string Name,
    string City,
    string? DenominationAbbreviation
);

public record CreateCongregationRequest(
    string Name,
    string City,
    string? LocationDetail,
    Guid? DenominationId,
    string? Modality,
    decimal? Latitude,
    decimal? Longitude
);

public record UpdateCongregationRequest(
    string Name,
    string City,
    string? LocationDetail,
    Guid? DenominationId,
    string? Modality,
    decimal? Latitude,
    decimal? Longitude
);
