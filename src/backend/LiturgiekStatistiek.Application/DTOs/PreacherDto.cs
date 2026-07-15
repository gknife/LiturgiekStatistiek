namespace LiturgiekStatistiek.Application.DTOs;

public record PreacherDto(
    Guid Id,
    string FullName,
    string? Denomination,
    string? City
);

public record PreacherSummaryDto(
    Guid Id,
    string FullName,
    string? City = null
);

public record CreatePreacherRequest(
    string FullName,
    Guid? DenominationId,
    string? City
);

public record UpdatePreacherRequest(
    string FullName,
    Guid? DenominationId,
    string? City
);
