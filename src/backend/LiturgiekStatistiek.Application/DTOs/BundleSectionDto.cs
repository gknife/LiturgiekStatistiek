namespace LiturgiekStatistiek.Application.DTOs;

public record BundleSectionDto(
    Guid Id,
    Guid BundleId,
    string Value,
    int SortOrder,
    bool IsDefault,
    bool IsActive
);

public record CreateBundleSectionRequest(
    string Value,
    int SortOrder,
    bool IsDefault
);

public record UpdateBundleSectionRequest(
    string Value,
    int SortOrder,
    bool IsDefault,
    bool IsActive
);
