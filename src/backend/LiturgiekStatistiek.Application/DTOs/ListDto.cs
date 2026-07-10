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
    bool IsActive,
    int? LiturgicalElementType = null,
    string? CreatedBy = null,
    DateTime? CreatedAt = null,
    string? ModifiedBy = null,
    DateTime? ModifiedAt = null
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
    bool IsActive,
    int? LiturgicalElementType = null
);
