using LiturgiekStatistiek.Application.DTOs.Queries;

namespace LiturgiekStatistiek.Application.Interfaces;

public interface IAdvancedQueryService
{
    /// <summary>Returns the whitelisted fields/operators the builder UI may offer.</summary>
    AdvancedQuerySchema GetSchema();

    /// <summary>Executes a single advanced query and returns a list or aggregate result.</summary>
    Task<QueryResult> ExecuteAsync(AdvancedQueryDefinition definition, CancellationToken ct = default);

    /// <summary>Executes multiple named queries and combines them into one comparison result.</summary>
    Task<QueryResult> CompareAsync(IReadOnlyList<AdvancedQueryDefinition> definitions, CancellationToken ct = default);
}
