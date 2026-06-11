namespace LiturgiekStatistiek.Application.DTOs;

public record PreacherDto(
    Guid Id,
    string FullName,
    string? Title,
    string? Denomination,
    string? City
);

public record PreacherSummaryDto(
    Guid Id,
    string FullName,
    string? Title
);

public record CreatePreacherRequest(
    string FullName,
    string? Title,
    Guid? DenominationId,
    string? City
);

public record UpdatePreacherRequest(
    string FullName,
    string? Title,
    Guid? DenominationId,
    string? City
);
