using LiturgiekStatistiek.Application.DTOs;
using LiturgiekStatistiek.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LiturgiekStatistiek.Api.Auth;

namespace LiturgiekStatistiek.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PreachersController : ControllerBase
{
    private readonly IPreacherService _preacherService;

    public PreachersController(IPreacherService preacherService)
    {
        _preacherService = preacherService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<PreacherDto>>> GetPreachers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null)
    {
        var result = await _preacherService.GetPreachersAsync(page, pageSize, search);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PreacherDto>> GetPreacher(Guid id)
    {
        var result = await _preacherService.GetPreacherByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<PreacherSummaryDto>>> SearchPreachers([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q)) return Ok(new List<PreacherSummaryDto>());
        var result = await _preacherService.SearchPreachersAsync(q);
        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<PreacherDto>> CreatePreacher([FromBody] CreatePreacherRequest request)
    {
        var userId = User.GetDisplayName();
        var result = await _preacherService.CreatePreacherAsync(request, userId);
        return CreatedAtAction(nameof(GetPreacher), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<PreacherDto>> UpdatePreacher(Guid id, [FromBody] UpdatePreacherRequest request)
    {
        var userId = User.GetDisplayName();
        var result = await _preacherService.UpdatePreacherAsync(id, request, userId);
        if (result == null) return NotFound();
        return Ok(result);
    }
}
