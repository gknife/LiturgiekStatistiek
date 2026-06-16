namespace LiturgiekStatistiek.Application.DTOs.Queries;

public record SavedQueryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    /// <summary>Serialized <see cref="AdvancedQueryDefinition"/> (JSON).</summary>
    public string QueryParameters { get; init; } = string.Empty;
    public bool IsPublic { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record SaveQueryRequest
{
    public string Name { get; init; } = string.Empty;
    public string QueryParameters { get; init; } = string.Empty;
    public bool IsPublic { get; init; }
}
