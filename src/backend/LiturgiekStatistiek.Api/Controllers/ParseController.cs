using LiturgiekStatistiek.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LiturgiekStatistiek.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ParseController : ControllerBase
{
    private readonly ILlmService _llmService;

    public ParseController(ILlmService llmService)
    {
        _llmService = llmService;
    }

    [HttpPost("liturgy")]
    public async Task<ActionResult<LlmLiturgyParseResult>> ParseLiturgy([FromBody] ParseLiturgyRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest(new { message = "Tekst is vereist." });

        var result = await _llmService.ParseLiturgyTextAsync(request.Text, ct);
        return Ok(result);
    }
}

public record ParseLiturgyRequest
{
    public string Text { get; init; } = string.Empty;
}
