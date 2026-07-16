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
    decimal? Longitude,
    Guid? DenominationId = null,
    int ServiceCount = 0,
    List<CongregationPastorDto>? Pastors = null
);

public record CongregationPastorDto(
    Guid PreacherId,
    string FullName,
    string? City,
    bool IsPrimary
);

public record CongregationPastorInput(
    Guid PreacherId,
    bool IsPrimary
);

public record CongregationSummaryDto(
    Guid Id,
    string Name,
    string City,
    string? DenominationAbbreviation,
    Guid? DenominationId = null,
    List<CongregationPastorDto>? Pastors = null
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
    decimal? Longitude,
    List<CongregationPastorInput>? Pastors = null
);
