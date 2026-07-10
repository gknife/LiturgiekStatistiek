using LiturgiekStatistiek.Application.DTOs;
using LiturgiekStatistiek.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LiturgiekStatistiek.Api.Auth;

namespace LiturgiekStatistiek.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ListsController : ControllerBase
{
    private readonly IListService _listService;

    public ListsController(IListService listService)
    {
        _listService = listService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ListDefinitionDto>>> GetAllLists()
    {
        var result = await _listService.GetAllListsAsync();
        return Ok(result);
    }

    [HttpGet("{name}")]
    public async Task<ActionResult<ListDefinitionDto>> GetListByName(string name)
    {
        var result = await _listService.GetListByNameAsync(name);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ListDefinitionDto>> CreateList([FromBody] CreateListDefinitionRequest request)
    {
        var userId = User.GetDisplayName();
        var result = await _listService.CreateListDefinitionAsync(request, userId);
        return CreatedAtAction(nameof(GetListByName), new { name = result.Name }, result);
    }

    [HttpPost("items")]
    [Authorize]
    public async Task<ActionResult<ListItemDto>> AddListItem([FromBody] CreateListItemRequest request)
    {
        var userId = User.GetDisplayName();
        var result = await _listService.AddListItemAsync(request, userId);
        return Created($"/api/lists/items/{result.Id}", result);
    }

    [HttpPut("items/{id:guid}")]
    [Authorize]
    public async Task<ActionResult<ListItemDto>> UpdateListItem(Guid id, [FromBody] UpdateListItemRequest request)
    {
        var userId = User.GetDisplayName();
        var result = await _listService.UpdateListItemAsync(id, request, userId);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpDelete("items/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteListItem(Guid id)
    {
        var userId = User.GetDisplayName();
        var success = await _listService.DeleteListItemAsync(id, userId);
        if (!success) return NotFound();
        return NoContent();
    }
}
