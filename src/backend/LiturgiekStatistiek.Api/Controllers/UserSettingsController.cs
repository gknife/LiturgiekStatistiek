using LiturgiekStatistiek.Application.DTOs;
using LiturgiekStatistiek.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LiturgiekStatistiek.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserSettingsController : ControllerBase
{
    private readonly IUserSettingsService _service;

    public UserSettingsController(IUserSettingsService service)
    {
        _service = service;
    }

    private string UserId => User.Identity?.Name ?? "unknown";

    [HttpGet]
    public async Task<ActionResult<UserSettingsDto>> Get(CancellationToken ct)
    {
        var settings = await _service.GetForUserAsync(UserId, ct);
        // Return an empty object when nothing is stored yet so the client can merge defaults.
        return Ok(settings ?? new UserSettingsDto("{}"));
    }

    [HttpPut]
    public async Task<ActionResult<UserSettingsDto>> Put([FromBody] UpdateUserSettingsRequest request, CancellationToken ct)
    {
        if (request?.SettingsJson is null)
            return BadRequest(new { message = "SettingsJson is vereist." });

        var saved = await _service.UpsertAsync(UserId, request, ct);
        return Ok(saved);
    }
}
