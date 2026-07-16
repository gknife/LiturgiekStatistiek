using LiturgiekStatistiek.Application.DTOs;
using LiturgiekStatistiek.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LiturgiekStatistiek.Api.Auth;

namespace LiturgiekStatistiek.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CongregationsController : ControllerBase
{
    private readonly ICongregationService _congregationService;

    public CongregationsController(ICongregationService congregationService)
    {
        _congregationService = congregationService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<CongregationDto>>> GetCongregations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null)
    {
        var result = await _congregationService.GetCongregationsAsync(page, pageSize, search);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CongregationDto>> GetCongregation(Guid id)
    {
        var result = await _congregationService.GetCongregationByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<CongregationSummaryDto>>> SearchCongregations([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q)) return Ok(new List<CongregationSummaryDto>());
        var result = await _congregationService.SearchCongregationsAsync(q);
        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<CongregationDto>> CreateCongregation([FromBody] CreateCongregationRequest request)
    {
        var userId = User.GetDisplayName();
        var result = await _congregationService.CreateCongregationAsync(request, userId);
        return CreatedAtAction(nameof(GetCongregation), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<CongregationDto>> UpdateCongregation(Guid id, [FromBody] UpdateCongregationRequest request)
    {
        var userId = User.GetDisplayName();
        var result = await _congregationService.UpdateCongregationAsync(id, request, userId);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteCongregation(Guid id)
    {
        var userId = User.GetDisplayName();
        var outcome = await _congregationService.DeleteCongregationAsync(id, userId);
        return outcome switch
        {
            DeleteOutcome.Deleted => NoContent(),
            DeleteOutcome.NotFound => NotFound(),
            DeleteOutcome.HasReferences => Conflict(new { message = "Gemeente heeft nog gekoppelde diensten en kan niet worden verwijderd." }),
            _ => StatusCode(500)
        };
    }
}
