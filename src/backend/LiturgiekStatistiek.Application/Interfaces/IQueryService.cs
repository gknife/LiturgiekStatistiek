using LiturgiekStatistiek.Application.DTOs.Queries;

namespace LiturgiekStatistiek.Application.Interfaces;

public interface IQueryService
{
    Task<List<QueryTemplate>> GetTemplatesAsync();
    Task<QueryResult> ExecuteTemplateAsync(string templateId, Dictionary<string, string> parameters, CancellationToken ct = default);
    Task<QueryResult> ExecuteNaturalLanguageAsync(string query, CancellationToken ct = default);
}
