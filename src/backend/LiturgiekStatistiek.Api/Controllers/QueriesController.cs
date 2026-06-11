using LiturgiekStatistiek.Application.DTOs.Queries;
using LiturgiekStatistiek.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LiturgiekStatistiek.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QueriesController : ControllerBase
{
    private readonly IQueryService _queryService;

    public QueriesController(IQueryService queryService)
    {
        _queryService = queryService;
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
            var result = await _queryService.ExecuteNaturalLanguageAsync(request.NaturalLanguageQuery, ct);
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
