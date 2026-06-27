using LiturgiekStatistiek.Application.DTOs;
using LiturgiekStatistiek.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LiturgiekStatistiek.Api.Controllers;

[ApiController]
[Route("api/recent-searches")]
[Authorize]
public class RecentSearchesController : ControllerBase
{
    private readonly IRecentSearchService _service;

    public RecentSearchesController(IRecentSearchService service)
    {
        _service = service;
    }

    private string UserId => User.Identity?.Name ?? "unknown";

    [HttpGet]
    public async Task<ActionResult<List<RecentSearchDto>>> Get([FromQuery] int max = 10, CancellationToken ct = default)
    {
        return Ok(await _service.GetForUserAsync(UserId, max, ct));
    }

    [HttpPost]
    public async Task<ActionResult<RecentSearchDto>> Add([FromBody] AddRecentSearchRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request?.QueryText))
            return BadRequest(new { message = "QueryText is vereist." });

        return Ok(await _service.AddAsync(UserId, request, ct));
    }

    [HttpDelete]
    public async Task<IActionResult> Clear(CancellationToken ct)
    {
        await _service.ClearAsync(UserId, ct);
        return NoContent();
    }
}
