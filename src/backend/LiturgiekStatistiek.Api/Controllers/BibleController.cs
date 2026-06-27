using LiturgiekStatistiek.Application.DTOs;
using LiturgiekStatistiek.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LiturgiekStatistiek.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BibleController : ControllerBase
{
    private readonly IBibleService _bibleService;

    public BibleController(IBibleService bibleService)
    {
        _bibleService = bibleService;
    }

    /// <summary>Returns the 66 canonical books, optionally with names resolved for a translation.</summary>
    [HttpGet("books")]
    public async Task<ActionResult<List<BibleBookDto>>> GetBooks([FromQuery] string? translation, CancellationToken ct)
    {
        var books = await _bibleService.GetBooksAsync(translation, ct);
        return Ok(books);
    }
}
