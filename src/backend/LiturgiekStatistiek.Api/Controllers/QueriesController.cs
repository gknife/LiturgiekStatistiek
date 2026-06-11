using LiturgiekStatistiek.Application.DTOs.Queries;
using LiturgiekStatistiek.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LiturgiekStatistiek.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QueriesController : ControllerBase
{
    private readonly IQueryService _queryService;
    private readonly ILlmService _llmService;

    public QueriesController(IQueryService queryService, ILlmService llmService)
    {
        _queryService = queryService;
        _llmService = llmService;
    }

    [HttpGet("templates")]
    public async Task<ActionResult<List<QueryTemplate>>> GetTemplates()
    {
        var templates = await _queryService.GetTemplatesAsync();
        return Ok(templates);
    }

    [HttpPost("execute")]
    public async Task<ActionResult<QueryResult>> ExecuteQuery([FromBody] QueryRequest request, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(request.NaturalLanguageQuery))
        {
            var parseResult = await _llmService.ParseNaturalLanguageQueryAsync(request.NaturalLanguageQuery, ct);
            if (!parseResult.Success)
            {
                return Ok(new QueryResult
                {
                    Title = "Kon vraag niet verwerken",
                    Description = parseResult.ErrorMessage ?? "Onbekende fout",
                });
            }

            var result = await _queryService.ExecuteTemplateAsync(
                parseResult.TemplateId!, parseResult.Parameters!, ct);
            return Ok(result);
        }

        if (!string.IsNullOrEmpty(request.TemplateId) && request.Parameters != null)
        {
            var result = await _queryService.ExecuteTemplateAsync(request.TemplateId, request.Parameters, ct);
            return Ok(result);
        }

        return BadRequest(new { message = "Geef een templateId met parameters of een natuurlijke taalvraag op." });
    }
}
