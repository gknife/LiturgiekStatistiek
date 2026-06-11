namespace LiturgiekStatistiek.Application.DTOs;

public record ContentPageDto(
    Guid Id,
    string Slug,
    string TitleNl,
    string ContentMarkdown,
    string? ModifiedBy,
    DateTime? ModifiedAt
);

public record UpdateContentPageRequest(
    string TitleNl,
    string ContentMarkdown
);
