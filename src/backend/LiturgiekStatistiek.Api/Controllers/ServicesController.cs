using LiturgiekStatistiek.Application.DTOs;
using LiturgiekStatistiek.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LiturgiekStatistiek.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServicesController : ControllerBase
{
    private readonly IServiceService _serviceService;

    public ServicesController(IServiceService serviceService)
    {
        _serviceService = serviceService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<ServiceSummaryDto>>> GetServices(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? congregationId = null,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null)
    {
        var result = await _serviceService.GetServicesAsync(page, pageSize, congregationId, fromDate, toDate);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ServiceDto>> GetService(Guid id)
    {
        var result = await _serviceService.GetServiceByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ServiceDto>> CreateService([FromBody] CreateServiceRequest request)
    {
        var userId = User.Identity?.Name ?? "unknown";
        var result = await _serviceService.CreateServiceAsync(request, userId);
        return CreatedAtAction(nameof(GetService), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<ServiceDto>> UpdateService(Guid id, [FromBody] UpdateServiceRequest request)
    {
        var userId = User.Identity?.Name ?? "unknown";
        var result = await _serviceService.UpdateServiceAsync(id, request, userId);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteService(Guid id)
    {
        var success = await _serviceService.DeleteServiceAsync(id);
        if (!success) return NotFound();
        return NoContent();
    }
}
