namespace LiturgiekStatistiek.Application.DTOs;

public record ListDefinitionDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystemList,
    List<ListItemDto> Items
);

public record ListItemDto(
    Guid Id,
    string Value,
    string? Abbreviation,
    int SortOrder,
    bool IsActive
);

public record CreateListDefinitionRequest(
    string Name,
    string? Description
);

public record CreateListItemRequest(
    Guid ListDefinitionId,
    string Value,
    string? Abbreviation,
    int SortOrder
);

public record UpdateListItemRequest(
    string Value,
    string? Abbreviation,
    int SortOrder,
    bool IsActive
);
