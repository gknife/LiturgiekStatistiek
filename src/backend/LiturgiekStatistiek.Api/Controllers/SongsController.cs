using LiturgiekStatistiek.Application.DTOs;
using LiturgiekStatistiek.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LiturgiekStatistiek.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SongsController : ControllerBase
{
    private readonly ISongService _songService;

    public SongsController(ISongService songService)
    {
        _songService = songService;
    }

    [HttpGet("bundle/{bundleId:guid}")]
    public async Task<ActionResult<PaginatedResult<SongDto>>> GetSongsByBundle(
        Guid bundleId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _songService.GetSongsByBundleAsync(bundleId, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SongDto>> GetSong(Guid id)
    {
        var result = await _songService.GetSongByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("bundle/{bundleId:guid}/number/{number:int}")]
    public async Task<ActionResult<SongDto>> GetSongByNumber(Guid bundleId, int number)
    {
        var result = await _songService.GetSongByNumberAsync(bundleId, number);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<SongDto>> CreateSong([FromBody] CreateSongRequest request)
    {
        var userId = User.Identity?.Name ?? "unknown";
        var result = await _songService.CreateSongAsync(request, userId);
        return CreatedAtAction(nameof(GetSong), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<SongDto>> UpdateSong(Guid id, [FromBody] UpdateSongRequest request)
    {
        var userId = User.Identity?.Name ?? "unknown";
        var result = await _songService.UpdateSongAsync(id, request, userId);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteSong(Guid id)
    {
        var success = await _songService.DeleteSongAsync(id);
        if (!success) return NotFound();
        return NoContent();
    }
}
