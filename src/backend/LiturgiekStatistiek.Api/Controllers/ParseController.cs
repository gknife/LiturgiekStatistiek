using LiturgiekStatistiek.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LiturgiekStatistiek.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ParseController : ControllerBase
{
    private readonly ILlmService _llmService;
    private readonly ILiturgyParser _parser;
    private readonly IUrlImportService _urlImportService;

    public ParseController(ILlmService llmService, ILiturgyParser parser, IUrlImportService urlImportService)
    {
        _llmService = llmService;
        _parser = parser;
        _urlImportService = urlImportService;
    }

    /// <summary>
    /// Parses pasted liturgy text. Uses the deterministic rule-based parser by default;
    /// falls back to the AI parser only when it is configured and the deterministic
    /// parser found no elements.
    /// </summary>
    [HttpPost("liturgy")]
    public async Task<ActionResult<LlmLiturgyParseResult>> ParseLiturgy([FromBody] ParseLiturgyRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest(new { message = "Tekst is vereist." });

        var parsed = _parser.Parse(request.Text, request.Title);

        if (parsed.Elements.Count == 0 && _llmService.IsConfigured)
        {
            var aiResult = await _llmService.ParseLiturgyTextAsync(request.Text, ct);
            if (aiResult.Success && aiResult.Data != null)
                return Ok(aiResult);
        }

        return Ok(new LlmLiturgyParseResult { Success = true, Data = parsed });
    }

    /// <summary>Imports a liturgy from a broadcast page URL (server-side fetch + parse).</summary>
    [HttpPost("url")]
    public async Task<ActionResult<LlmLiturgyParseResult>> ImportUrl([FromBody] ParseUrlRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            return BadRequest(new { message = "URL is vereist." });

        var result = await _urlImportService.ImportAsync(request.Url, ct);
        if (!result.Success)
            return Ok(new LlmLiturgyParseResult { Success = false, ErrorMessage = result.ErrorMessage });

        return Ok(new LlmLiturgyParseResult { Success = true, Data = result.Data });
    }
}

public record ParseLiturgyRequest
{
    public string Text { get; init; } = string.Empty;
    public string? Title { get; init; }
}

public record ParseUrlRequest
{
    public string Url { get; init; } = string.Empty;
}
