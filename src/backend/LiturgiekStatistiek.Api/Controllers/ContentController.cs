using LiturgiekStatistiek.Application.DTOs;
using LiturgiekStatistiek.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LiturgiekStatistiek.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContentController : ControllerBase
{
    private readonly IContentService _contentService;

    public ContentController(IContentService contentService)
    {
        _contentService = contentService;
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<ContentPageDto>> GetContent(string slug)
    {
        var result = await _contentService.GetContentBySlugAsync(slug);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPut("{slug}")]
    [Authorize]
    public async Task<ActionResult<ContentPageDto>> UpdateContent(string slug, [FromBody] UpdateContentPageRequest request)
    {
        var userId = User.Identity?.Name ?? "unknown";
        var result = await _contentService.UpdateContentAsync(slug, request, userId);
        if (result == null) return NotFound();
        return Ok(result);
    }
}
