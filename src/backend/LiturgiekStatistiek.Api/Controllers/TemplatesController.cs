using LiturgiekStatistiek.Application.DTOs;
using LiturgiekStatistiek.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LiturgiekStatistiek.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TemplatesController : ControllerBase
{
    private readonly ITemplateService _templateService;

    public TemplatesController(ITemplateService templateService)
    {
        _templateService = templateService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ServiceTemplateSummaryDto>>> GetTemplates()
    {
        return Ok(await _templateService.GetTemplatesAsync());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ServiceTemplateDto>> GetTemplate(Guid id)
    {
        var result = await _templateService.GetTemplateByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ServiceTemplateDto>> CreateTemplate([FromBody] CreateServiceTemplateRequest request)
    {
        var userId = User.Identity?.Name ?? "unknown";
        var result = await _templateService.CreateTemplateAsync(request, userId);
        return CreatedAtAction(nameof(GetTemplate), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<ServiceTemplateDto>> UpdateTemplate(Guid id, [FromBody] CreateServiceTemplateRequest request)
    {
        var userId = User.Identity?.Name ?? "unknown";
        var result = await _templateService.UpdateTemplateAsync(id, request, userId);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteTemplate(Guid id)
    {
        var success = await _templateService.DeleteTemplateAsync(id);
        if (!success) return NotFound();
        return NoContent();
    }

    /// <summary>
    /// Resolves the best-matching template for the given selectors and returns its
    /// onderdelen as ready-to-use service element requests for pre-filling a new service.
    /// </summary>
    [HttpGet("instantiate")]
    public async Task<ActionResult<List<CreateServiceElementRequest>>> Instantiate(
        [FromQuery] Guid? denominationId = null,
        [FromQuery] Guid? congregationId = null,
        [FromQuery] int? timeOfDay = null,
        [FromQuery] Guid? occasionId = null)
    {
        var result = await _templateService.InstantiateAsync(denominationId, congregationId, timeOfDay, occasionId);
        if (result == null) return NoContent();
        return Ok(result);
    }
}
