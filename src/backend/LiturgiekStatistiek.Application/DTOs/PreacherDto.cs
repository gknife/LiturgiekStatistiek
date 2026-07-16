namespace LiturgiekStatistiek.Application.DTOs;

public record PreacherDto(
    Guid Id,
    string FullName,
    string? Denomination,
    string? City,
    Guid? DenominationId = null,
    Guid? TitleId = null,
    string? Title = null,
    int ServiceCount = 0
);

public record PreacherSummaryDto(
    Guid Id,
    string FullName,
    string? City = null,
    string? Title = null,
    string? Denomination = null
);

public record CreatePreacherRequest(
    string FullName,
    Guid? DenominationId,
    string? City,
    Guid? TitleId = null
);

public record UpdatePreacherRequest(
    string FullName,
    Guid? DenominationId,
    string? City,
    Guid? TitleId = null
);
