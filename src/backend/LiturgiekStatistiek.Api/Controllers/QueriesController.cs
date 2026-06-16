using LiturgiekStatistiek.Application.DTOs.Queries;
using LiturgiekStatistiek.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LiturgiekStatistiek.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QueriesController : ControllerBase
{
    private readonly IQueryService _queryService;
    private readonly ILlmService _llmService;
    private readonly IAdvancedQueryService _advancedQueryService;
    private readonly ISavedQueryService _savedQueryService;

    public QueriesController(
        IQueryService queryService,
        ILlmService llmService,
        IAdvancedQueryService advancedQueryService,
        ISavedQueryService savedQueryService)
    {
        _queryService = queryService;
        _llmService = llmService;
        _advancedQueryService = advancedQueryService;
        _savedQueryService = savedQueryService;
    }

    [HttpGet("templates")]
    public async Task<ActionResult<List<QueryTemplate>>> GetTemplates()
    {
        var templates = await _queryService.GetTemplatesAsync();
        return Ok(templates);
    }

    [HttpGet("ai-status")]
    public ActionResult<LlmStatus> GetAiStatus()
    {
        return Ok(_llmService.GetStatus());
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

    // ---------------- Advanced query builder ----------------

    [HttpGet("advanced/schema")]
    public ActionResult<AdvancedQuerySchema> GetAdvancedSchema()
    {
        return Ok(_advancedQueryService.GetSchema());
    }

    [HttpPost("advanced/execute")]
    public async Task<ActionResult<QueryResult>> ExecuteAdvanced([FromBody] AdvancedQueryDefinition definition, CancellationToken ct)
    {
        try
        {
            var result = await _advancedQueryService.ExecuteAsync(definition, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return Ok(new QueryResult
            {
                Title = "Kon query niet uitvoeren",
                Description = ex.Message
            });
        }
    }

    [HttpPost("advanced/compare")]
    public async Task<ActionResult<QueryResult>> CompareAdvanced([FromBody] CompareQueriesRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _advancedQueryService.CompareAsync(request.Queries, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return Ok(new QueryResult
            {
                Title = "Kon vergelijking niet uitvoeren",
                Description = ex.Message
            });
        }
    }

    // ---------------- Saved queries (authenticated) ----------------

    [HttpGet("saved")]
    [Authorize]
    public async Task<ActionResult<List<SavedQueryDto>>> GetSavedQueries(CancellationToken ct)
    {
        var userId = User.Identity?.Name ?? "unknown";
        return Ok(await _savedQueryService.GetForUserAsync(userId, ct));
    }

    [HttpGet("saved/{id:guid}")]
    [Authorize]
    public async Task<ActionResult<SavedQueryDto>> GetSavedQuery(Guid id, CancellationToken ct)
    {
        var userId = User.Identity?.Name ?? "unknown";
        var item = await _savedQueryService.GetByIdAsync(id, userId, ct);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost("saved")]
    [Authorize]
    public async Task<ActionResult<SavedQueryDto>> CreateSavedQuery([FromBody] SaveQueryRequest request, CancellationToken ct)
    {
        var userId = User.Identity?.Name ?? "unknown";
        var created = await _savedQueryService.CreateAsync(request, userId, ct);
        return CreatedAtAction(nameof(GetSavedQuery), new { id = created.Id }, created);
    }

    [HttpDelete("saved/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteSavedQuery(Guid id, CancellationToken ct)
    {
        var userId = User.Identity?.Name ?? "unknown";
        var success = await _savedQueryService.DeleteAsync(id, userId, ct);
        if (!success) return NotFound();
        return NoContent();
    }
}
